using System;
using System.Threading.Tasks;
using Dapper;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    internal interface ITokenRepository
    {
        Task Create(string value, int userId, Token.TokenPurpose purpose, DateTime expiry);

        Task<Token?> GetByValue(string value);

        Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose);
    }

    internal class TokenRepository : ITokenRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public TokenRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task Create(string value, int userId, Token.TokenPurpose purpose, DateTime expiry)
        {
            var entity = new Token
            {
                UserId = userId,
                Value = value,
                Created = DateTime.UtcNow,
                Expiry = expiry,
                Purpose = purpose
            };

            await using var connection = await _connectionProvider.Get();

            await connection.InsertEntity(entity);
        }

        public async Task<Token?> GetByValue(string value)
        {
            await using var connection = await _connectionProvider.Get();

            var result = await connection.QueryFirstOrDefaultAsync<Token?>("SELECT * FROM tokens WHERE value = @value", new { value });

            return result;
        }

        public async Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose)
        {
            await using var connection = await _connectionProvider.Get();

            var result = await connection.QueryFirstOrDefaultAsync<Token?>(
                "SELECT * FROM tokens WHERE user_id = @userId AND purpose = @purpose ORDER BY created DESC LIMIT 1;", 
                new { userId = userId, purpose = purpose });

            return result;
        }
    }
}