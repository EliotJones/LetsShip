namespace PriceFalcon.Infrastructure
{
    public enum EmailSendResult
    {
        Success = 1,
        Error = 2,
        InvalidRecipient = 3,
        ServiceUnavailable = 4,
        QuotaExceeded = 5
    }
}