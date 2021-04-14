using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Web.Services
{
    public class RequestLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestLogQueue _queue;
        private readonly PriceFalconConfig _config;

        public RequestLogMiddleware(RequestDelegate next, RequestLogQueue queue, PriceFalconConfig config)
        {
            _next = next;
            _queue = queue;
            _config = config;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var ip = context.Request.Headers["X-Forwarded-For"];

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

            if (url == null || url.Contains("/create/track", StringComparison.OrdinalIgnoreCase) && context.Response.StatusCode / 100 == 2)
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

            var uri = new Uri(url);

            if (_config.Environment != EnvironmentType.Development && uri.Host.Contains("pricefalcon", StringComparison.OrdinalIgnoreCase))
            {
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