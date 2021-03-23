using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.DraftJobs
{
    public class RequestNewJobToken : IRequest<string?>
    {
        public string Email { get; set; } = string.Empty;
    }

    internal class RequestNewJobTokenHandler : IRequestHandler<RequestNewJobToken, string?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;

        public RequestNewJobTokenHandler(IUserRepository userRepository, ITokenService tokenService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public async Task<string?> Handle(RequestNewJobToken request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmail(request.Email);

            if (user == null || !user.IsVerified)
            {
                return null;
            }

            var lastToken = await _tokenService.GetLastToken(user.Id, Token.TokenPurpose.CreateJob);

            if (lastToken != null)
            {
                // todo: expiry check
            }

            var token = await _tokenService.GenerateToken(user.Id, Token.TokenPurpose.CreateJob, DateTime.UtcNow.AddDays(10));

            var safeToken = WebUtility.UrlEncode(token.token);
            var message = $@"<p>Hi!</p><p>You requested a new job for PriceFalcon, use this link to create a new PriceFalcon monitoring job: 
                <a href='http://localhost:5220/create/{safeToken}'>Get started</a>
                </p>";

            await _emailService.Send(user.Email, "Create a new job", message);

            return token.token;
        }
    }
}
