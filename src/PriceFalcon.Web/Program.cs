using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PriceFalcon.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Running with UTC time: {DateTime.UtcNow}.");

            var host = CreateHostBuilder(args).Build();

            var config = host.Services.GetRequiredService<IConfiguration>();

            foreach (var c in config.AsEnumerable())
            {
                Console.WriteLine(c.Key + " = " + c.Value);
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
