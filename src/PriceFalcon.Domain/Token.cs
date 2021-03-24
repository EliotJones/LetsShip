using System;

namespace PriceFalcon.Domain
{
    public class Token
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Value { get; set; } = string.Empty;

        public bool IsUsed { get; set; }

        public TokenPurpose Purpose { get; set; }

        public DateTime Expiry { get; set; }

        public DateTime Created { get; set; }

        public enum TokenPurpose
        {
            ValidateEmail = 1,
            ViewAccount = 2,
            Unsubscribe = 3,
            CreateJob = 4,
            MonitorJob = 5
        }
    }
}