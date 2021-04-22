using OpenQA.Selenium;

namespace PriceFalcon.Crawler
{
    internal static class CountrySelector
    {
        public static void SelectCountryIfNeeded(IWebDriver driver, string country)
        {
            var countryElements = driver.FindElements(By.XPath("//*[contains(@class, 'country')]"));

            var havingCountryText = driver.FindElements(By.XPath(XPathExtensions.ContainsText(country)));

            if (havingCountryText.Count > 0)
            {
                
            }
        }
    }

    internal static class XPathExtensions
    {
        public static string ContainsText(string text, string element = "*")
        {
            return $"//{element}[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{text.ToLowerInvariant()}')]";
        }
    }
}
