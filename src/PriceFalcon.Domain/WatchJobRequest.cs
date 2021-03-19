namespace PriceFalcon.Domain
{
    public class WatchJobRequest
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Url { get; set; }

        public JobStatus Status { get; set; }

        public string Message { get; set; }

        public enum JobStatus
        {
            Pending = 1,
            Processing = 2,
            Complete = 3,
            Failed=  4
        }
    }
}
