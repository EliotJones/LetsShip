using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Web.Services
{
    public class RequestLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestLogQueue _queue;
        private readonly ILogger<RequestLogMiddleware> _logger;

        public RequestLogMiddleware(RequestDelegate next, RequestLogQueue queue, ILogger<RequestLogMiddleware> logger)
        {
            _next = next;
            _queue = queue;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var ip = context.Request.Headers["X-Forwarded-For"];

            var logHeaders = new StringBuilder();
            foreach (var header in context.Request.Headers)
            {
                logHeaders.AppendLine($"{header.Key}: {header.Value}");
            }

            _logger.LogInformation(logHeaders.ToString());

            string ipAddress;
            if (ip.Count == 0)
            {
                var remoteIp = context.Connection.RemoteIpAddress;

                ipAddress = remoteIp?.ToString() ?? "::1";
            }
            else
            {
                ipAddress = ip[0];
            }

            await _next(context);

            stopwatch.Stop();

            var url = context.Request.GetDisplayUrl();

            if (url.Contains("/create/track", StringComparison.OrdinalIgnoreCase) && context.Response.StatusCode / 100 == 2)
            {
                // Skip the successful polling endpoints.
                return;
            }

            if (url.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".php", StringComparison.OrdinalIgnoreCase))
            {
                // Skip resources and automated script-spam.
                return;
            }

            _queue.Add(new RequestLog
            {
                Created = DateTime.UtcNow,
                IpAddress = ipAddress,
                Url = url,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                StatusCode = context.Response.StatusCode,
                Method = context.Request.Method
            });
        }
    }
}