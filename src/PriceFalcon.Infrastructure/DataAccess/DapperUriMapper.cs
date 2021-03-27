using System;
using System.Data;
using Dapper;

namespace PriceFalcon.Infrastructure.DataAccess
{
    internal class DapperUriMapper : SqlMapper.TypeHandler<Uri>
    {
        public override void SetValue(IDbDataParameter parameter, Uri value)
        {
            parameter.Value = value.ToString();
        }

        public override Uri Parse(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Could not convert value to Uri.");
            }

            return new Uri(value.ToString() ?? "about:empty");
        }
    }
}
