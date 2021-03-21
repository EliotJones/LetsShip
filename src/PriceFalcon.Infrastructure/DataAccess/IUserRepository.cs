using System;
using System.Threading.Tasks;
using Dapper;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IUserRepository
    {
        Task<User?> GetByEmail(string? email);

        Task<User?> GetById(int id);

        Task<User> Create(string email);

        Task<string> GetEmailById(int userId);

        Task SetVerified(int userId);
    }

    internal class UserRepository : IUserRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public UserRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<User?> GetByEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await using var connection = await _connectionProvider.Get();

            return await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM users WHERE email = @email;", new { email });
        }

        public async Task<User?> GetById(int id)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM users WHERE id = @id;", new { id });
        }

        public async Task<User> Create(string email)
        {
            await using var connection = await _connectionProvider.Get();

            var user = new User
            {
                Created = DateTime.UtcNow,
                Email = email,
                IsVerified = false
            };

            await connection.InsertEntity(user);

            return user;
        }

        public async Task<string> GetEmailById(int userId)
        {
            await using var connection = await _connectionProvider.Get();

            return await connection.QueryFirstOrDefaultAsync<string>("SELECT email FROM users WHERE id = @id;", new { id = userId });
        }

        public async Task SetVerified(int userId)
        {
            await using var connection = await _connectionProvider.Get();

            await connection.ExecuteAsync("UPDATE users SET is_verified = TRUE WHERE id = @id;", new { id = userId });
        }
    }
}
