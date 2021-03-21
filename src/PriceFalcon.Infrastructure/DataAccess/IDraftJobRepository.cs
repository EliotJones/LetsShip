using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IDraftJobRepository
    {
        Task<DraftJob> Create(Uri website, int userId);

        Task<IReadOnlyList<DraftJobLog>> GetLogs(int jobId);

        Task<DraftJob> GetById(int jobId);

        Task<IReadOnlyList<DraftJob>> GetJobsInStatus(DraftJobStatus status);

        Task SetMonitoringTokenId(int jobId, int monitoringTokenId);
    }

    internal class DraftJobRepository : IDraftJobRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public DraftJobRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<DraftJob> Create(Uri website, int userId)
        {
            await using var connection = await _connectionProvider.Get();

            var created = DateTime.UtcNow;

            var entity = new DraftJob
            {
                CrawledHtml = null,
                UserId = userId,
                Created = created,
                Status = DraftJobStatus.Pending,
                Url = website
            };

            await connection.InsertEntity(entity);

            var log = new DraftJobLog
            {
                Created = created,
                DraftJobId = entity.Id,
                Message = $"Created for website {website} by the user.",
                Status = DraftJobStatus.Pending
            };

            await connection.InsertEntity(log);

            return entity;
        }

        public async Task<IReadOnlyList<DraftJobLog>> GetLogs(int jobId)
        {
            await using var connection = await _connectionProvider.Get();

            var results = await connection.QueryAsync<DraftJobLog>(
                "SELECT * FROM draft_job_logs WHERE draft_job_id = @id ORDER BY created ASC;",
                new { id = jobId });

            return results.ToList();
        }

        public async Task<DraftJob> GetById(int jobId)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.QuerySingleAsync<DraftJob>(
                "SELECT * FROM draft_jobs WHERE id = @id;",
                new { id = jobId });
        }

        public Task<IReadOnlyList<DraftJob>> GetJobsInStatus(DraftJobStatus status)
        {
            throw new NotImplementedException();
        }

        public async Task SetMonitoringTokenId(int jobId, int monitoringTokenId)
        {
            await using var connection = await _connectionProvider.Get();

            await connection.ExecuteAsync(
                "UPDATE draft_jobs SET monitoring_token_id = @tokenId WHERE id = @id;",
                new { id = jobId, tokenId = monitoringTokenId });
        }
    }
}
