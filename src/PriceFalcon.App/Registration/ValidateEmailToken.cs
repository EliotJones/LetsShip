using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Registration
{
    public class EmailTokenValidateResult
    {
        public static readonly EmailTokenValidateResult Error = new EmailTokenValidateResult();

        public bool IsSuccess { get; }

        public string? Email { get; set; }

        private EmailTokenValidateResult()
        {
            IsSuccess = false;
        }

        private EmailTokenValidateResult(string email)
        {
            IsSuccess = true;
            Email = email;
        }

        public static EmailTokenValidateResult Success(string email) => new EmailTokenValidateResult(email);
    }

    public class ValidateEmailToken : IRequest<EmailTokenValidateResult>
    {
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
            var tokenValid = await _tokenService.ValidateToken(request.Token, Token.TokenPurpose.ValidateEmail);

            if (tokenValid.Status != TokenValidationStatus.Success || !tokenValid.UserId.HasValue)
            {
                return EmailTokenValidateResult.Error;
            }

            await _userRepository.SetVerified(tokenValid.UserId.Value);
            await _tokenService.Revoke(request.Token);

            var userEmail = await _userRepository.GetEmailById(tokenValid.UserId.Value);

            return EmailTokenValidateResult.Success(userEmail);
        }
    }
}
