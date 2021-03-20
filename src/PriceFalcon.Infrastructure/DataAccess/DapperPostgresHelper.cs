using System;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace PriceFalcon.Infrastructure.DataAccess
{
    internal static class DapperPostgresHelper
    {
        public static async Task InsertEntity<T>(this DbConnection connection, T entity)
        {
            var parameters = new DynamicParameters();

            var properties = typeof(T).GetProperties();

            var table = ToSnakeCase(typeof(T).Name.ToLowerInvariant()) + "s";

            var query = new StringBuilder("insert into ").Append(table).Append(" (");

            var idSetter = default(MethodInfo?);

            for (var i = 0; i < properties.Length; i++)
            {
                var isLast = i == properties.Length - 1;
                var property = properties[i];
                var name = ToSnakeCase(property.Name);

                if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
                {
                    idSetter = property.GetSetMethod(true);
                    continue;
                }

                var getter = property.GetGetMethod(true);

                if (getter == null)
                {
                    continue;
                }

                query.Append(name);

                parameters.Add(name, getter.Invoke(entity, null));

                if (!isLast)
                {
                    query.Append(", ");
                }
            }

            query.Append(") values (");

            foreach (var parameter in parameters.ParameterNames)
            {
                query.Append("@").Append(parameter).Append(", ");
            }

            query.Remove(query.Length - 2, 2);

            query.Append(") returning id;");

            var sql = query.ToString();

            var result = await connection.ExecuteScalarAsync<int>(sql, parameters);

            if (idSetter != null)
            {
                idSetter.Invoke(entity, new object[] {result});
            }
        }

        private static string ToSnakeCase(string s)
        {
            var result = string.Empty;
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        result += '_';
                    }

                    result += char.ToLowerInvariant(c);
                }
                else
                {
                    result += c;
                }
            }

            return result;
        }
    }
}
