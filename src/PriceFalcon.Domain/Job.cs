using System;

namespace PriceFalcon.Domain
{
    public class Job
    {
        public int Id { get; set; }

        public int DraftJobId { get; set; }

        public Uri Url { get; set; } = new Uri("about:empty");

        public int UserId { get; set; }

        public string Selector { get; set; } = string.Empty;

        public DateTime NextDueDate { get; set; }

        public int TokenId { get; set; }

        public decimal? StartPrice { get; set; }

        public string Xpath { get; set; } = string.Empty;

        public JobStatus Status { get; set; }

        public DateTime Created { get; set; }
    }

    public enum JobStatus
    {
        Deleted = 0,
        Active = 1,
        Paused = 2
    }
}