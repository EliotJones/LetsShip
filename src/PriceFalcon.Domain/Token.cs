using System;

namespace PriceFalcon.Domain
{
    public class Token
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Value { get; set; }

        public DateTime Expiry { get; set; }

        public bool IsUsed { get; set; }
        public enum Purpose
        {
            ValidateEmail = 1,
            ViewAccount = 2,
            Unsubscribe = 3,
            CreateJob = 4
        }
    }
}