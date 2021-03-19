using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App
{
    public class GetUserByEmail : IRequest<User?>
    {
        public string Email { get; set; } = string.Empty;
    }

    internal class GetUserByEmailHandler : IRequestHandler<GetUserByEmail, User?>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByEmailHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> Handle(GetUserByEmail request, CancellationToken cancellationToken)
        {
            return await _userRepository.GetByEmail(request.Email);
        }
    }
}
