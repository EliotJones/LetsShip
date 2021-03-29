using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
// ReSharper disable AccessToDisposedClosure

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

        [Fact]
        [Trait("Category", "Integration-Selenium")]
        public async Task GetPageSource_ArgosWithCookieBanner_AcceptsCookies()
        {
            using var crawler = GetCrawler(1);

            if (crawler == null)
            {
                return;
            }

            var source = await crawler.GetPageSource(new Uri("https://www.argos.co.uk/product/6205524"), _ => Task.CompletedTask, CancellationToken.None);

            Assert.NotNull(source);
        }

        [Fact]
        [Trait("Category", "Integration-Selenium")]
        public async Task GetPageSource_InParallel_GetsHtml()
        {
            using var crawler = GetCrawler(2);

            if (crawler == null)
            {
                return;
            }

            var tasks = new Task[2];
            tasks[0] = Task.Run(() => crawler.GetPageSource(new Uri("https://www.argos.co.uk/product/6205524"), 
                _ => Task.CompletedTask, 
                CancellationToken.None));
            tasks[1] = Task.Run(() => crawler.GetPageSource(new Uri("https://www.argos.co.uk/product/4490070"),
                _ => Task.CompletedTask,
                CancellationToken.None));

            await Task.WhenAll(tasks);
        }

        private static FirefoxCrawler? GetCrawler(int maxTasks = 1)
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

            var crawler = new FirefoxCrawler(path, maxTasks, false);

            return crawler;
        }
    }
}
