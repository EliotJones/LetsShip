using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Web.Services
{
    public class RequestLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestLogQueue _queue;

        public RequestLogMiddleware(RequestDelegate next, RequestLogQueue queue)
        {
            _next = next;
            _queue = queue;
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

            if (url.Contains("/create/track", StringComparison.OrdinalIgnoreCase) && context.Response.StatusCode / 100 == 2)
            {
                // Skip the polling endpoints.
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