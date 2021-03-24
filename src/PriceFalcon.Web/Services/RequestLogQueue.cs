using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Web.Services
{
    public class RequestLogQueue
    {
        private readonly ConcurrentBag<RequestLog> _requests = new ConcurrentBag<RequestLog>();

        public bool HasRequests => _requests.Count > 0;

        public void Add(RequestLog log)
        {
            if (_requests.Count > 200)
            {
                return;
            }

            _requests.Add(log);
        }

        public IReadOnlyList<RequestLog> Empty()
        {
            var results = _requests.ToArray();

            _requests.Clear();

            return results;
        }
    }

    public class RequestLogQueueConsumer : BackgroundService
    {
        private readonly RequestLogQueue _queue;
        private readonly IRequestLogRepository _repository;
        private readonly ILogger<RequestLogQueueConsumer> _logger;

        public RequestLogQueueConsumer(RequestLogQueue queue, IRequestLogRepository repository, ILogger<RequestLogQueueConsumer> logger)
        {
            _queue = queue;
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_queue.HasRequests)
                    {
                        var items = _queue.Empty();
                        _logger.LogDebug($"Flushing {items.Count} requests from the requests queue.");
                        await _repository.CreateAll(items);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, $"Failed to flush messages from the requests queue.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
