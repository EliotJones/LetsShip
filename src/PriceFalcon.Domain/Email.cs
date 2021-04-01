using System;

namespace PriceFalcon.Domain
{
    public class Email
    {
        public int Id { get; set; }

        public string Recipient { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public DateTime Created { get; set; }
    }
}
