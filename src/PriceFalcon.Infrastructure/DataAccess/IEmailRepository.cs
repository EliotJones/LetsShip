using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    internal interface IEmailRepository
    {
        Task Create(string body, string to, string subject);

        Task<IReadOnlyList<Email>> GetAll();

        Task<IReadOnlyList<Email>> GetAllSentToEmailInPeriod(string recipient, DateTime fromInclusive);

        Task<int> GetSentTodayCount();
    }

    internal class EmailRepository : IEmailRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public EmailRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task Create(string body, string to, string subject)
        {
            await using var connection = await _connectionProvider.Get();

            var userId = await connection.QueryFirstOrDefaultAsync<int?>("SELECT id FROM users WHERE email = @email;", new { email = to });

            var email = new Email
            {
                Body = body,
                Created = DateTime.UtcNow,
                Recipient = to,
                Subject = subject,
                UserId = userId
            };

            await connection.InsertEntity(email);
        }

        public async Task<IReadOnlyList<Email>> GetAll()
        {
            await using var connection = await _connectionProvider.Get();

            var results = await connection.QueryAsync<Email>(
                "SELECT * FROM emails ORDER BY created DESC;");

            return results.ToList();
        }

        public async Task<IReadOnlyList<Email>> GetAllSentToEmailInPeriod(string recipient, DateTime fromInclusive)
        {
            await using var connection = await _connectionProvider.Get();

            var results = await connection.QueryAsync<Email>(
                "SELECT * FROM emails WHERE recipient = @recipient AND created >= @fromInclusive;",
                new { recipient = recipient, fromInclusive = fromInclusive });

            return results.ToList();
        }

        public async Task<int> GetSentTodayCount()
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM emails WHERE created >= @date;",
                new {date = DateTime.UtcNow.Date});
        }
    }
}