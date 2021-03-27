using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceFalcon.App.Jobs;

namespace PriceFalcon.Web.Services
{
    public class EmailPriceChangeNotifyService : BackgroundService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EmailPriceChangeNotifyService> _logger;

        public EmailPriceChangeNotifyService(IMediator mediator, ILogger<EmailPriceChangeNotifyService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _mediator.Send(new NotifyJobListeners(), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to notify email price changes.");
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch
                {
                    break;
                }
            }
        }
    }
}