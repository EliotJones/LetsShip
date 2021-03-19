using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App
{
    public class EmailTokenValidateResult
    {
        public bool IsSuccess { get; }

        public string? JobCreateToken { get; }

        public EmailTokenValidateResult(string jobCreateToken)
        {
            IsSuccess = true;
            JobCreateToken = jobCreateToken;
        }

        public EmailTokenValidateResult()
        {
            IsSuccess = false;
        }
    }

    public class ValidateEmailToken : IRequest<EmailTokenValidateResult>
    {
        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
    }

    internal class ValidateEmailTokenHandler : IRequestHandler<ValidateEmailToken, EmailTokenValidateResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public ValidateEmailTokenHandler(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<EmailTokenValidateResult> Handle(ValidateEmailToken request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmail(request.Email);

            if (user == null)
            {
                return new EmailTokenValidateResult();
            }

            var tokenValid = await _tokenService.ValidateToken(request.Token, Token.Purpose.ValidateEmail);

            if (tokenValid != TokenValidationResult.Success)
            {
                return new EmailTokenValidateResult();
            }

            var jobToken = await _tokenService.GenerateToken(user.Id, Token.Purpose.CreateJob, DateTime.UtcNow.AddDays(16));

            return new EmailTokenValidateResult(jobToken);
        }
    }
}
