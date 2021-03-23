using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PriceFalcon.Crawler;
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

                    var isDev = hostContext.HostingEnvironment.IsDevelopment();

                    string geckoPath;
                    if (isDev)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            geckoPath = Path.Combine(hostContext.HostingEnvironment.ContentRootPath, "Drivers", "Windows", "geckodriver.exe");
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            geckoPath = Path.Combine(hostContext.HostingEnvironment.ContentRootPath, "Drivers", "Linux", "geckodriver");
                        }
                        else
                        {
                            throw new InvalidOperationException("JobCrawler is not support on your operating system, use Windows or Linux.");
                        }
                    }
                    else
                    {
                        geckoPath = config.GeckoDriverPath;
                    }

                    services.AddSingleton<ICrawler>(new FirefoxCrawler(geckoPath, 1, !isDev));

                    Bootstrapper.Start(services, hostContext.Configuration);
                    services.AddHostedService<Worker>();
                });
    }
}