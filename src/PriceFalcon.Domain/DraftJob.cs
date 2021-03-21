using System;

namespace PriceFalcon.Domain
{
    public class DraftJob
    {
        public int Id { get; set; }

        public Uri Url { get; set; } = new Uri("about:empty");

        public string? CrawledHtml { get; set; }

        public int UserId { get; set; }

        public DraftJobStatus Status { get; set; }

        public DateTime Created { get; set; }
    }

    public class DraftJobLog
    {
        public int Id { get; set; }

        public int DraftJobId { get; set; }

        public string? Message { get; set; }

        public DraftJobStatus Status { get; set; }

        public DateTime Created { get; set; }
    }

    public enum DraftJobStatus
    {
        Pending = 1,
        Queued = 2,
        Processing = 3,
        Completed = 4,
        Failed=  5
    }
}
