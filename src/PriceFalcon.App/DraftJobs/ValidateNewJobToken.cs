using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;

namespace PriceFalcon.App.DraftJobs
{
    public class ValidateNewJobTokenResult
    {
        public int? UserId { get; set; }

        public bool IsValid { get; set; }
    }

    public class ValidateNewJobToken : IRequest<ValidateNewJobTokenResult>
    {
        public string Token { get; }

        public ValidateNewJobToken(string token)
        {
            Token = token;
        }
    }

    internal class ValidateNewJobTokenHandler : IRequestHandler<ValidateNewJobToken, ValidateNewJobTokenResult>
    {
        private readonly ITokenService _tokenService;

        public ValidateNewJobTokenHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task<ValidateNewJobTokenResult> Handle(ValidateNewJobToken request, CancellationToken cancellationToken)
        {
            var token = await _tokenService.ValidateToken(request.Token, Token.TokenPurpose.CreateJob);

            if (token.Status != TokenValidationStatus.Success)
            {
                return new ValidateNewJobTokenResult
                {
                    IsValid = false
                };
            }

            return new ValidateNewJobTokenResult
            {
                IsValid = true,
                UserId = token.UserId!.Value
            };
        }
    }
}
