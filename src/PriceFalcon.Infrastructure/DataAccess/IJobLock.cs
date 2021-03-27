using System;
using System.Threading.Tasks;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IJobLock : IDisposable
    {
        JobStatus Status { get; }

        DateTime Due { get; }

        void Abandon();

        Task Complete(decimal price, string message);

        Task CompleteWithError(string message);
    }
}