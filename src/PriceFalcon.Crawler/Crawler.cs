using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace PriceFalcon.Crawler
{
    public interface ICrawler : IDisposable
    {
        Task<string> GetPageSource(Uri website, Func<string, Task> asyncLogger, CancellationToken cancellationToken);
    }

    public class FirefoxCrawler : ICrawler
    {
        private readonly string _geckoDriverPath;
        private readonly bool _isHeadless;
        private readonly SemaphoreSlim _semaphore;
        private readonly Stack<FirefoxDriver> _drivers;

        public FirefoxCrawler(string geckoDriverPath, int maxTasks, bool isHeadless)
        {
            _geckoDriverPath = geckoDriverPath;
            _isHeadless = isHeadless;
            _semaphore = new SemaphoreSlim(maxTasks, maxTasks);
            _drivers = new Stack<FirefoxDriver>(maxTasks);
        }

        public async Task<string> GetPageSource(Uri website, Func<string, Task> asyncLogger, CancellationToken cancellationToken)
        {
            // FirefoxDriver expects the directory but not the file as the path.
            var actualPath = Path.GetDirectoryName(_geckoDriverPath);

            if (!File.Exists(_geckoDriverPath))
            {
                throw new ArgumentException($"No geckodriver exists at the provided path: {_geckoDriverPath}.");
            }

            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_drivers.TryPop(out var driver))
                {
                    var options = new FirefoxOptions
                    {
                        PageLoadStrategy = PageLoadStrategy.Normal,
                        AcceptInsecureCertificates = true,
                        UnhandledPromptBehavior = UnhandledPromptBehavior.Accept
                    };

                    if (_isHeadless)
                    {
                        options.AddArgument("--headless");
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    driver = new FirefoxDriver(FirefoxDriverService.CreateDefaultService(actualPath), options, TimeSpan.FromSeconds(120));

                    cancellationToken.ThrowIfCancellationRequested();
                }

                try
                {
                    var stopwatch = Stopwatch.StartNew();

                    await asyncLogger($"About to load: {website}");

                    driver.Navigate().GoToUrl(website);

                    cancellationToken.ThrowIfCancellationRequested();

                    await asyncLogger($"[{Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} s] Waiting for website at \"{driver.Url}\" to load fully...");

                    new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(
                        d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                    cancellationToken.ThrowIfCancellationRequested();

                    var isSameUrl = string.Equals(driver.Url, website.ToString(), StringComparison.OrdinalIgnoreCase);

                    if (!isSameUrl)
                    {
                        driver.Navigate().GoToUrl(website);

                        isSameUrl = string.Equals(driver.Url, website.ToString(), StringComparison.OrdinalIgnoreCase);

                        if (!isSameUrl)
                        {
                            await asyncLogger(
                                $"Loaded URL did not match the one you provided. We loaded: {driver.Url}. It's possible the target site's location detection kicked in and chose the wrong region.");
                        }
                    }

                    await asyncLogger($"[{Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} s] Page fully loaded.");

                    var html = driver.PageSource;

                    cancellationToken.ThrowIfCancellationRequested();

                    await asyncLogger($"[{Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} s] Page crawled successfully.");

                    return html;
                }
                finally
                {
                    _drivers.Push(driver);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();

            foreach (var driver in _drivers)
            {
                driver.Dispose();
            }
        }
    }
}
