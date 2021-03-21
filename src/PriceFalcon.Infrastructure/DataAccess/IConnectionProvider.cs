using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace PriceFalcon.Infrastructure.DataAccess
{
    internal interface IConnectionProvider
    {
        Task<DbConnection> Get();
    }

    internal class DefaultConnectionProvider : IConnectionProvider
    {
        private readonly string _connectionString;

        public DefaultConnectionProvider(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            }

            _connectionString = connectionString;
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            Dapper.SqlMapper.AddTypeHandler(new DapperUriMapper());
        }

        public async Task<DbConnection> Get()
        {
            var connection = new Npgsql.NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
