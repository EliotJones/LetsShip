using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace PriceFalcon.Crawler
{
    public interface ICrawler
    {
        Task<string> GetPageSource(Uri website, CancellationToken cancellationToken);
    }

    public class FirefoxCrawler : ICrawler
    {
        private readonly string _geckoDriverPath;
        private readonly int _maxTasks;
        private readonly bool _isHeadless;

        public FirefoxCrawler(string geckoDriverPath, int maxTasks, bool isHeadless)
        {
            _geckoDriverPath = geckoDriverPath;
            _maxTasks = maxTasks;
            _isHeadless = isHeadless;
        }

        public Task<string> GetPageSource(Uri website, CancellationToken cancellationToken)
        {
            // FirefoxDriver expects the directory but not the file as the path.
            var actualPath = Path.GetDirectoryName(_geckoDriverPath);

            var options = new FirefoxOptions
            {
                PageLoadStrategy = PageLoadStrategy.Normal,
                AcceptInsecureCertificates = true,
                UnhandledPromptBehavior = UnhandledPromptBehavior.Accept
            };
            options.AddArgument("--headless");

            using var driver = new FirefoxDriver(FirefoxDriverService.CreateDefaultService(actualPath), options, TimeSpan.FromSeconds(50));

            driver.Navigate().GoToUrl(website);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(
                d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            var html = driver.PageSource;

            driver.Close();

            return Task.FromResult(html);
        }
    }
}
