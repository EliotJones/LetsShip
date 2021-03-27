using System;

namespace PriceFalcon.Domain
{
    public class JobRun
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string? Message { get; set; }

        public decimal? Price { get; set; }

        public JobRunStatus Status { get; set; }

        public bool IsNotified { get; set; }

        public int? EmailId { get; set; }

        public DateTime Created { get; set; }
    }

    public enum JobRunStatus
    {
        Succeeded = 1,
        Failed = 2
    }
}