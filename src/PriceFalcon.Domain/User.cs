using System;

namespace PriceFalcon.Domain
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public bool IsVerified { get; set; }

        public DateTime Created { get; set; }
    }
}
