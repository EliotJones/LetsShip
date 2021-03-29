using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IJobRepository
    {
        Task<int> GetJobCountForUser(int userId);

        Task Create(int tokenId, HtmlElementSelection selector, decimal price, string xpath, int draftJobId);

        Task<Job?> GetByDraftJobId(int draftJobId);

        Task<IReadOnlyList<Job>> GetJobsDue();

        Task<IJobLock> AcquireJobLock(int jobId, CancellationToken cancellationToken);

        Task<IReadOnlyList<int>> GetJobIdsWithRunsNotNotified();

        Task<IReadOnlyList<JobRun>> GetJobRunsByJobId(int jobId);

        Task MarkAllJobRunsNotifiedForJob(int jobId);

        Task<Job> GetById(int id);
    }

    internal class JobRepository : IJobRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public JobRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<int> GetJobCountForUser(int userId)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM jobs WHERE user_id = @userId;", new {userId = userId});
        }

        public async Task Create(int tokenId, HtmlElementSelection selector, decimal price, string xpath, int draftJobId)
        {
            await using var connection = await _connectionProvider.Get();

            var random = new Random(DateTime.UtcNow.Millisecond);

            // TODO: implement
            const string sql = @"
                INSERT INTO jobs 
                (
                    draft_job_id, 
                    url,
                    user_id,
                    selector,
                    next_due_date,
                    token_id,
                    start_price,
                    status,
                    xpath,
                    created
                )
                SELECT  dj.id,
                        dj.url,
                        dj.user_id,
                        CAST(@selector as json),
                        @nextDue,
                        @tokenId,
                        @price,
                        @status,
                        @xpath,
                        @created
                FROM draft_jobs as dj
                WHERE dj.id = @draftJobId;";

            await connection.ExecuteAsync(
                sql,
                new
                {
                    draftJobId = draftJobId,
                    selector = JsonSerializer.Serialize(selector),
                    nextDue = DateTime.UtcNow.AddHours(random.Next(3, 5)).AddMinutes(random.Next(25)),
                    tokenId = tokenId,
                    price = price,
                    status = JobStatus.Active,
                    xpath = xpath,
                    created = DateTime.UtcNow
                });
        }

        public async Task<Job?> GetByDraftJobId(int draftJobId)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.QueryFirstOrDefaultAsync<Job>(
                "SELECT * FROM jobs WHERE draft_job_id = @draftJobId;",
                new {draftJobId = draftJobId});
        }

        public async Task<IReadOnlyList<Job>> GetJobsDue()
        {
            await using var connection = await _connectionProvider.Get();

            var results = await connection.QueryAsync<Job>(
                "SELECT * FROM jobs WHERE next_due_date <= @date;",
                new {date = DateTime.UtcNow});

            return results.ToList();
        }

        public async Task<IJobLock> AcquireJobLock(int jobId, CancellationToken cancellationToken)
        {
            var connection = await _connectionProvider.Get();

            try
            {
                var transaction = await connection.BeginTransactionAsync(cancellationToken);

                var job = await connection.QueryFirstAsync<Job>(
                    "SELECT * FROM jobs WHERE id = @id FOR UPDATE;",
                    new {id = jobId},
                    transaction);

                return new TransactionJobLock(job, transaction, connection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public async Task<IReadOnlyList<int>> GetJobIdsWithRunsNotNotified()
        {
            await using var connection = await _connectionProvider.Get();

            var results = await connection.QueryAsync<int>(
                "SELECT distinct(job_id) FROM job_runs WHERE is_notified = FALSE;");

            return results.ToList();
        }

        public async Task<IReadOnlyList<JobRun>> GetJobRunsByJobId(int jobId)
        {
            await using var connection = await _connectionProvider.Get();

            var results = await connection.QueryAsync<JobRun>(
                "SELECT * FROM job_runs WHERE job_id = @id ORDER BY created DESC;",
                new {id = jobId});

            return results.ToList();
        }

        public async Task MarkAllJobRunsNotifiedForJob(int jobId)
        {
            await using var connection = await _connectionProvider.Get();

            await connection.ExecuteAsync(
                "UPDATE job_runs SET is_notified = TRUE WHERE job_id = @id;",
                new {id = jobId});
        }

        public async Task<Job> GetById(int id)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.QuerySingleAsync<Job>(
                "SELECT * FROM jobs WHERE id = @id;",
                new {id = id});
        }
    }

    internal class TransactionJobLock : IJobLock
    {
        private readonly Job _job;
        private readonly IDbTransaction _transaction;
        private readonly IDbConnection _connection;

        public JobStatus Status => _job.Status;

        public DateTime Due => _job.NextDueDate;

        public TransactionJobLock(Job job, IDbTransaction transaction, IDbConnection connection)
        {
            _job = job;
            _transaction = transaction;
            _connection = connection;
        }

        public void Abandon()
        {
            _transaction.Rollback();
        }

        public async Task Complete(decimal price, string message)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);

            var nextDue = DateTime.UtcNow.AddHours(random.Next(3, 5)).AddMinutes(random.Next(25));

            var entity = new JobRun
            {
                Status = JobRunStatus.Succeeded,
                Created = DateTime.UtcNow,
                JobId = _job.Id,
                Message = message,
                Price = price
            };

            await _connection.InsertEntity(entity);

            await _connection.ExecuteAsync(
                "UPDATE jobs SET next_due_date = @date WHERE id = @id;",
                new {date = nextDue, id = _job.Id},
                _transaction);

            _transaction.Commit();
        }

        public async Task CompleteWithError(string message)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);

            var nextDue = DateTime.UtcNow.AddMinutes(random.Next(5, 25));

            var entity = new JobRun
            {
                Status = JobRunStatus.Failed,
                Created = DateTime.UtcNow,
                JobId = _job.Id,
                Message = message
            };

            await _connection.InsertEntity(entity);

            await _connection.ExecuteAsync(
                "UPDATE jobs SET next_due_date = @date WHERE id = @id;",
                new { date = nextDue, id = _job.Id },
                _transaction);

            _transaction.Commit();
        }

        public void Dispose()
        {
            _transaction.Dispose();

            _connection.Dispose();
        }
    }
}
