using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App
{
    public class RequestNewJobToken : IRequest<string?>
    {
        public string Email { get; set; } = string.Empty;
    }

    internal class RequestNewJobTokenHandler : IRequestHandler<RequestNewJobToken, string?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public RequestNewJobTokenHandler(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<string?> Handle(RequestNewJobToken request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmail(request.Email);

            if (user == null || !user.IsVerified)
            {
                return null;
            }

            var lastToken = await _tokenService.GetLastToken(user.Id, Token.Purpose.CreateJob);

            if (lastToken != null)
            {
                // todo: expiry check
            }

            return await _tokenService.GenerateToken(user.Id, Token.Purpose.CreateJob, DateTime.UtcNow.AddDays(10));
        }
    }
}
