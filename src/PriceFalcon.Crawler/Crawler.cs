using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using PriceFalcon.Domain;

namespace PriceFalcon.Crawler
{
    public class PriceJobResult
    {
        public bool IsSuccess { get; set; }

        public decimal? Price { get; set; }

        public string Log { get; set; } = string.Empty;
    }

    public interface ICrawler : IDisposable
    {
        Task<string> GetPageSource(Uri website, Func<string, Task> asyncLogger, CancellationToken cancellationToken);

        Task<PriceJobResult> GetPrice(Uri website, HtmlElementSelection selection, string xpath, CancellationToken cancellationToken);
    }

    public class FirefoxCrawler : ICrawler
    {
        private readonly string _geckoDriverPath;
        private readonly bool _isHeadless;
        private readonly SemaphoreSlim _semaphore;
        private readonly Stack<FirefoxDriver> _drivers;

        public FirefoxCrawler(string geckoDriverPath, int maxTasks, bool isHeadless)
        {
            // FirefoxDriver expects the directory but not the file as the path.
            var actualPath = Path.GetDirectoryName(geckoDriverPath);

            if (!File.Exists(geckoDriverPath))
            {
                throw new ArgumentException($"No geckodriver exists at the provided path: {geckoDriverPath}.");
            }

            _geckoDriverPath = actualPath;
            _isHeadless = isHeadless;
            _semaphore = new SemaphoreSlim(maxTasks, maxTasks);
            _drivers = new Stack<FirefoxDriver>(maxTasks);
        }

        public async Task<string> GetPageSource(Uri website, Func<string, Task> asyncLogger, CancellationToken cancellationToken)
        {
            return await GetDriverAndRun(
                async driver =>
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
                },
                cancellationToken);
        }

        public async Task<PriceJobResult> GetPrice(Uri website, HtmlElementSelection selection, string xpath, CancellationToken cancellationToken)
        {
            var result = await GetDriverAndRun(
                driver =>
                {
                    var logger = new StringBuilder();
                    try
                    {

                        logger.AppendLine($"About to load: {website}");

                        driver.Navigate().GoToUrl(website);

                        cancellationToken.ThrowIfCancellationRequested();

                        logger.AppendLine($"Waiting for website at \"{driver.Url}\" to load fully...");

                        new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(
                            d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                        cancellationToken.ThrowIfCancellationRequested();

                        var isSameUrl = string.Equals(driver.Url, website.ToString(), StringComparison.OrdinalIgnoreCase);

                        if (!isSameUrl)
                        {
                            driver.Navigate().GoToUrl(website);

                            cancellationToken.ThrowIfCancellationRequested();

                            isSameUrl = string.Equals(driver.Url, website.ToString(), StringComparison.OrdinalIgnoreCase);

                            if (!isSameUrl)
                            {
                                logger.AppendLine($"Loaded URL did not match the one you provided. We loaded: {driver.Url}.");
                            }
                        }

                        logger.AppendLine("Page fully loaded.");

                        var elements = driver.FindElementsByXPath(xpath)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                            .ToList();

                        if (elements.Count == 0)
                        {
                            return Task.FromResult(
                                new PriceJobResult
                                {
                                    Log = logger.AppendLine($"Could not find the element by XPath: {xpath}.").ToString(),
                                    IsSuccess = false
                                });
                        }

                        if (elements.Count > 1)
                        {
                            logger.AppendLine($"More than one element found by XPath {xpath}, using the first.");
                        }

                        var element = elements[0];

                        if (!PriceCrawlValidator.TryGetPrice(element.Text, out var price))
                        {
                            return Task.FromResult(
                                new PriceJobResult
                                {
                                    Log = logger.AppendLine($"Could not find the price in the element with text: {element.Text}.").ToString(),
                                    IsSuccess = false
                                });
                        }

                        return Task.FromResult(
                            new PriceJobResult
                            {
                                Log = logger.ToString(),
                                IsSuccess = true,
                                Price = price
                            });
                    }
                    catch (Exception ex)
                    {
                        logger.AppendLine($"Error encountered: {ex}.");

                        return Task.FromResult(
                            new PriceJobResult
                            {
                                Log = logger.ToString(),
                                IsSuccess = false
                            });
                    }
                },
                cancellationToken);

            return result;
        }

        private async Task<T> GetDriverAndRun<T>(Func<FirefoxDriver, Task<T>> task, CancellationToken cancellationToken)
        {
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

                    driver = new FirefoxDriver(FirefoxDriverService.CreateDefaultService(_geckoDriverPath), options, TimeSpan.FromSeconds(120));

                    cancellationToken.ThrowIfCancellationRequested();
                }

                try
                {
                    var result = await task(driver);

                    return result;
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
                try
                {
                    driver.Close();
                    driver.Dispose();
                }
                catch
                {
                    // ignored.
                }
            }
        }
    }
}
