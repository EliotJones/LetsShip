using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PriceFalcon.Crawler.Tests
{
    public class CrawlerTests
    {
        [Fact]
        [Trait("Category", "Integration-Selenium")]
        public async Task GetPageSource_CourtsBarbados_GetsHtml()
        {
            using var crawler = GetCrawler();

            if (crawler == null)
            {
                return;
            }

            var html = await crawler.GetPageSource(
                new Uri(@"https://www.shopcourts.com/barbados/products.html/cell-phones-and-domestic-phones/smartphone-64gb-black.html"),
                _ => Task.CompletedTask,
                CancellationToken.None);

            Assert.NotNull(html);

            Assert.StartsWith("<html", html);
        }

        [Fact]
        [Trait("Category", "Integration-Selenium")]
        public async Task GetPageSource_SigmaAldrich_GetsHtml()
        {
            using var crawler = GetCrawler();

            if (crawler == null)
            {
                return;
            }

            var html = await crawler.GetPageSource(
                new Uri("https://www.sigmaaldrich.com/catalog/product/aldrich/779601?lang=en&region=GB"),
                _ => Task.CompletedTask,
                CancellationToken.None);

            Assert.NotNull(html);
        }

        private static FirefoxCrawler? GetCrawler()
        {
            string path;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drivers", "Windows", "geckodriver.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drivers", "Linux", "geckodriver");
            }
            else
            {
                return null;
            }

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"No geckodriver executable found at: {path}.");
            }

            var crawler = new FirefoxCrawler(path, 1, false);

            return crawler;
        }
    }
}
