using System.Threading.Tasks;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure.DataAccess
{
    public interface IUserRepository
    {
        Task<User?> GetByEmail(string? email);

        Task<User?> GetById(int id);

        Task<User> CreateUser(string email);
    }

    internal class UserRepository : IUserRepository
    {
        public Task<User?> GetByEmail(string? email)
        {
            throw new System.NotImplementedException();
        }

        public Task<User?> GetById(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<User> CreateUser(string email)
        {
            throw new System.NotImplementedException();
        }
    }
}
