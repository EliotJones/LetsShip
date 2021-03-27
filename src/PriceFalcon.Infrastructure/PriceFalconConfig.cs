using System;

namespace PriceFalcon.Infrastructure
{
    public class PriceFalconConfig
    {
        public EnvironmentType Environment { get; set; } = EnvironmentType.Development;

        public string SendGridApiKey { get; set; } = string.Empty;

        public string GeckoDriverPath { get; set; } = string.Empty;

        public Uri SiteUrl { get; set; } = new Uri("about:blank");
    }

    public enum EnvironmentType
    {
        Development = 1,
        Staging = 2,
        Production = 3
    }
}
