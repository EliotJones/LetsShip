using System;

namespace PriceFalcon.Domain
{
    public class Email
    {
        public int Id { get; set; }

        public string Recipient { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public int? UserId { get; set; }

        public DateTime Created { get; set; }
    }
}
