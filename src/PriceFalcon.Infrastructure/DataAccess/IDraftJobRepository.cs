using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IDraftJobLock : IDisposable
    {
        DraftJobStatus Status { get; }

        void Abandon();

        void Complete();

        Task SetStatus(DraftJobStatus status);

        Task SetHtml(string html);

        Task Log(string message, DraftJobStatus status);
    }

    internal class TransactionalDraftJobLock : IDraftJobLock
    {
        private readonly int _jobId;
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;
        private readonly DbConnection _nonTransactionConnection;

        public DraftJobStatus Status { get; }

        public TransactionalDraftJobLock(int jobId,
            DraftJobStatus status,
            IDbConnection connection,
            IDbTransaction transaction,
            DbConnection nonTransactionConnection)
        {
            _jobId = jobId;
            Status = status;
            _connection = connection;
            _transaction = transaction;
            _nonTransactionConnection = nonTransactionConnection;
        }

        public void Abandon()
        {
            _transaction.Rollback();
        }

        public void Complete()
        {
            _transaction.Commit();
        }

        public async Task SetStatus(DraftJobStatus status)
        {
            await _connection.ExecuteAsync(
                "UPDATE draft_jobs SET status = @status WHERE id = @id;",
                new {status = status, id = _jobId},
                _transaction);
        }

        public async Task SetHtml(string html)
        {
            await _connection.ExecuteAsync(
                "UPDATE draft_jobs SET crawled_html = @html WHERE id = @id;",
                new {html = html, id = _jobId},
                _transaction);
        }

        public async Task Log(string message, DraftJobStatus status)
        {
            var entity = new DraftJobLog
            {
                DraftJobId = _jobId,
                Created = DateTime.UtcNow,
                Message = message,
                Status = status
            };

            await _nonTransactionConnection.InsertEntity(entity);
        }

        public void Dispose()
        {
            _transaction.Dispose();
            _connection.Dispose();
            _nonTransactionConnection.Dispose();
        }

    }

    public interface IDraftJobRepository
    {
        Task<DraftJob> Create(Uri website, int userId);

        Task<IReadOnlyList<DraftJobLog>> GetLogs(int jobId);

        Task<DraftJob> GetById(int jobId);

        Task<IReadOnlyList<DraftJob>> GetJobsInStatus(DraftJobStatus status);

        Task SetMonitoringTokenId(int jobId, int monitoringTokenId);

        Task<IDraftJobLock> AcquireJobLock(int jobId, CancellationToken cancellationToken);

        Task<DraftJob?> GetByMonitoringToken(string token);
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

        public async Task<IReadOnlyList<DraftJob>> GetJobsInStatus(DraftJobStatus status)
        {
            await using var connection = await _connectionProvider.Get();

            var jobs = await connection.QueryAsync<DraftJob>(
                "SELECT * FROM draft_jobs WHERE status = @status ORDER BY created ASC;",
                new {status = status});

            return jobs.ToList();
        }

        public async Task SetMonitoringTokenId(int jobId, int monitoringTokenId)
        {
            await using var connection = await _connectionProvider.Get();

            await connection.ExecuteAsync(
                "UPDATE draft_jobs SET monitoring_token_id = @tokenId WHERE id = @id;",
                new { id = jobId, tokenId = monitoringTokenId });
        }

        public async Task<IDraftJobLock> AcquireJobLock(int jobId, CancellationToken cancellationToken)
        {
            var nonTransactionConnection = await _connectionProvider.Get();
            var connection = await _connectionProvider.Get();
            var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var status = await connection.QueryFirstOrDefaultAsync<DraftJobStatus>(
                new CommandDefinition(@"SELECT status FROM draft_jobs WHERE id = @id FOR UPDATE;",
                new { id = jobId },
                transaction,
                cancellationToken: cancellationToken));

            return new TransactionalDraftJobLock(jobId, status, connection, transaction, nonTransactionConnection);
        }

        public async Task<DraftJob?> GetByMonitoringToken(string token)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.QueryFirstOrDefaultAsync<DraftJob>(
                "SELECT dj.* FROM draft_jobs as dj INNER JOIN tokens as t ON t.id = dj.monitoring_token_id WHERE t.Value = @token;",
                new { token = token });

        }
    }
}
