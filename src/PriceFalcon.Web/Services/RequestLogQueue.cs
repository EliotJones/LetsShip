using System.Collections.Concurrent;
using System.Collections.Generic;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Web.Services
{
    public class RequestLogQueue
    {
        private readonly ConcurrentBag<RequestLog> _requests = new ConcurrentBag<RequestLog>();

        public bool HasRequests => _requests.Count > 0;

        public void Add(RequestLog log)
        {
            if (_requests.Count > 200)
            {
                return;
            }

            _requests.Add(log);
        }

        public IReadOnlyList<RequestLog> Empty()
        {
            var results = _requests.ToArray();

            _requests.Clear();

            return results;
        }
    }
}
