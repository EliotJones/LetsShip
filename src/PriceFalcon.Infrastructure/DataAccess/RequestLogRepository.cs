using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public class RequestLog
    {
        public int Id { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public long ElapsedMilliseconds { get; set; }

        public int? StatusCode { get; set; }

        public string Method { get; set; } = string.Empty;

        public DateTime Created { get; set; }
    }

    public interface IRequestLogRepository
    {
        Task CreateAll(IEnumerable<RequestLog> logs);
    }

    internal class RequestLogRepository : IRequestLogRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public RequestLogRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task CreateAll(IEnumerable<RequestLog> logs)
        {
            await using var connection = await _connectionProvider.Get();

            var items = (await connection.QueryAsync<RequestLog>("SELECT * FROM request_logs;")).ToList();

            // TODO: could use binary copy in.
            foreach (var log in logs)
            {
                await connection.InsertEntity(log);
            }
        }
    }
}
