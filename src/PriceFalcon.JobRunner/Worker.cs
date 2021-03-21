using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.JobRunner
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDraftJobRepository _draftJobRepository;

        public Worker(ILogger<Worker> logger, IDraftJobRepository draftJobRepository)
        {
            _logger = logger;
            _draftJobRepository = draftJobRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
