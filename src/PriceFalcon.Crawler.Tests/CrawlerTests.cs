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
                return;
            }

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"No geckodriver executable found at: {path}.");
            }

            var crawler = new FirefoxCrawler(path, 1, false);

            var html = await crawler.GetPageSource(
                new Uri(@"https://www.shopcourts.com/barbados/products.html/cell-phones-and-domestic-phones/smartphone-64gb-black.html"),
                CancellationToken.None);

            Assert.NotNull(html);
        }
    }
}
