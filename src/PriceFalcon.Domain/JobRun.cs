using System;

namespace PriceFalcon.Domain
{
    public class JobRun
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string? Message { get; set; }

        public decimal? Price { get; set; }

        public int Status { get; set; }

        public DateTime Created { get; set; }
    }
}