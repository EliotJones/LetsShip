using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PriceFalcon.Infrastructure;

namespace PriceFalcon.JobRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration.Get<PriceFalconConfig>();
                    config.SendGridApiKey = hostContext.Configuration.GetValue<string>("PRICE_FALCON_SENDGRID");

                    services.AddSingleton<PriceFalconConfig>();

                    Bootstrapper.Start(services, hostContext.Configuration);
                    services.AddHostedService<Worker>();
                });
    }
}
