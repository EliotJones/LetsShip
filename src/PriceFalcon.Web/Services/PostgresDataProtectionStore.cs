using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Web.Services
{
    /// <summary>
    /// Adapted from https://github.com/DBHow/DBGang.AspNetCore.DataProtection.
    /// </summary>
    public class PostgresDataProtectionStore : IXmlRepository
    {
        private readonly IDataProtectionKeyRepository _repository;

        public PostgresDataProtectionStore(IDataProtectionKeyRepository repository)
        {
            _repository = repository;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            var stored = _repository.GetAllSync();
            var results = new List<XElement>(stored.Count);

            foreach (var element in stored)
            {
                results.Add(XElement.Parse(element.Value));
            }

            return results;
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            _repository.InsertSync(friendlyName, element.ToString(SaveOptions.DisableFormatting));
        }
    }
}
