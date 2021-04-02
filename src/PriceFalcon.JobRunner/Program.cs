using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PriceFalcon.Crawler;
using PriceFalcon.Infrastructure;

namespace PriceFalcon.JobRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Running with UTC time: {DateTime.UtcNow}.");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration.Get<PriceFalconConfig>();
                    config.SendGridApiKey = hostContext.Configuration.GetValue<string>("PRICE_FALCON_SENDGRID");

                    services.AddSingleton<PriceFalconConfig>();

                    string geckoPath;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        geckoPath = Path.Combine(hostContext.HostingEnvironment.ContentRootPath, "Drivers", "Windows", "geckodriver.exe");
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        geckoPath = Path.Combine(hostContext.HostingEnvironment.ContentRootPath, "Drivers", "Linux", "geckodriver");
                    }
                    else if (!string.IsNullOrWhiteSpace(config.GeckoDriverPath))
                    {
                        geckoPath = config.GeckoDriverPath;
                    }
                    else
                    {
                        throw new InvalidOperationException("JobCrawler is not support on your operating system, use Windows or Linux.");
                    }

                    Console.WriteLine($"Using gecko driver from: {geckoPath}");

                    var isDev = hostContext.HostingEnvironment.IsDevelopment();
                    services.AddSingleton<ICrawler>(new FirefoxCrawler(geckoPath, isDev ? 1 : 3, isDev));

                    Bootstrapper.Start(services, hostContext.Configuration);
                    services.AddHostedService<Worker>();

                    services.AddLogging(
                        lb => lb.AddSimpleConsole(
                            sc =>
                            {
                                sc.ColorBehavior = Debugger.IsAttached ? LoggerColorBehavior.Enabled : LoggerColorBehavior.Disabled;
                                sc.UseUtcTimestamp = true;
                                sc.TimestampFormat = "[hh:mm:ss] ";
                            }));
                });
    }
}
