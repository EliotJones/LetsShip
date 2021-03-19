namespace PriceFalcon.Infrastructure
{
    public class PriceFalconConfig
    {
        public EnvironmentType Environment { get; set; } = EnvironmentType.Development;

        public string SendGridApiKey { get; set; } = string.Empty;
    }

    public enum EnvironmentType
    {
        Development = 1,
        Staging = 2,
        Production = 3
    }
}
