using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App
{
    public class CreateValidatedNewJobToken : IRequest<string>
    {
        public string Email { get; set; } = string.Empty;
    }

    internal class CreateValidatedNewJobTokenHandler : IRequestHandler<CreateValidatedNewJobToken, string>
    {
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;

        public CreateValidatedNewJobTokenHandler(ITokenService tokenService, IUserRepository userRepository)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
        }

        public async Task<string> Handle(CreateValidatedNewJobToken request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmail(request.Email);

            if (user?.IsVerified != true)
            {
                throw new InvalidOperationException($"Cannot create valid new job token because user {request.Email} has not been verified.");
            }

            var token = await _tokenService.GenerateToken(user.Id, Token.TokenPurpose.CreateJob, DateTime.UtcNow.AddDays(10));

            return token.token;
        }
    }
}
