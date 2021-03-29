using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IDataProtectionKeyRepository
    {
        DataProtectionKey InsertSync(string name, string value);

        IReadOnlyList<DataProtectionKey> GetAllSync();
    }

    internal class DataProtectionKeyRepository : IDataProtectionKeyRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public DataProtectionKeyRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public DataProtectionKey InsertSync(string name, string value)
        {
            using var connection = _connectionProvider.GetSync();

            var entity = new DataProtectionKey
            {
                Name = name,
                Value = value
            };

            var id = connection.ExecuteScalar<int>("INSERT INTO data_protection_keys (name, value) VALUES (@name, @value) RETURNING id;",
                new { name = name, value = value });

            entity.Id = id;

            return entity;
        }

        public IReadOnlyList<DataProtectionKey> GetAllSync()
        {
            using var connection = _connectionProvider.GetSync();

            var results = connection.Query<DataProtectionKey>("SELECT * FROM data_protection_keys;");

            return results.ToList();
        }
    }
}
