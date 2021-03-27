using System.Globalization;
using System.Text.RegularExpressions;

namespace PriceFalcon.Crawler
{
    public static class PriceCrawlValidator
    {
        private static readonly Regex PriceRegex = new Regex(@"\p{Sc}?\s*(?<price>(\d+[.,]?)+)");
        private static readonly Regex DecimalRegex = new Regex(@"(?<decimalsep>[.,])?\d{2}\b");
        private static readonly Regex ThousandsRegex = new Regex(@"(?<thousandssep>[.,])?\d{3}");

        public static bool TryGetPrice(string text, out decimal price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var matches = PriceRegex.Matches(text);

            if (matches.Count != 1)
            {
                return false;
            }

            var match = matches[0];

            if (!match.Success)
            {
                return false;
            }

            var priceGroup = match.Groups["price"];

            if (!priceGroup.Success || string.IsNullOrWhiteSpace(priceGroup.Value))
            {
                return false;
            }

            var priceStr = priceGroup.Value;

            if (!priceStr.Contains('.') && !priceStr.Contains(','))
            {
                price = decimal.Parse(priceStr, CultureInfo.InvariantCulture);
                return true;
            }

            var withoutThousands = priceStr;

            var thousandsSepMatch = ThousandsRegex.Match(priceStr);
            if (thousandsSepMatch.Success && thousandsSepMatch.Groups["thousandssep"].Success)
            {
                var thousandsVal = thousandsSepMatch.Groups["thousandssep"].Value;

                if (!string.IsNullOrWhiteSpace(thousandsVal))
                {
                    withoutThousands = withoutThousands.Replace(thousandsVal, string.Empty);
                }
            }

            var normalizedDecimal = withoutThousands;

            var decimalSepMatch = DecimalRegex.Match(priceStr);
            if (decimalSepMatch.Success && decimalSepMatch.Groups["decimalsep"].Success)
            {
                var decimalVal = decimalSepMatch.Groups["decimalsep"].Value;

                if (decimalVal == "." || decimalVal == ",")
                {
                    normalizedDecimal = normalizedDecimal.Replace(decimalVal, ".");
                }
            }

            price = decimal.Parse(normalizedDecimal, CultureInfo.InvariantCulture);

            return true;
        }
    }
}
