using System;
using System.Text.Json;
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
                    crawled_html,
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
                        dj.crawled_html,
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
    }
}
