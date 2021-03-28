using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.DraftJobs
{
    public class RequestNewJobTokenResult
    {
        public static readonly RequestNewJobTokenResult NoVerifiedUser = new RequestNewJobTokenResult(StatusReason.NoVerifiedUser);
        public static readonly RequestNewJobTokenResult TooManyRequests = new RequestNewJobTokenResult(StatusReason.TooManyRequests);

        public string? Token { get; }

        public StatusReason Status { get; }

        public RequestNewJobTokenResult(string token)
        {
            Token = token;
            Status = StatusReason.Created;
        }

        private RequestNewJobTokenResult(StatusReason status)
        {
            Status = status;
        }

        public enum StatusReason
        {
            Created = 1,
            NoVerifiedUser = 2,
            TooManyRequests = 3
        }
    }

    public class RequestNewJobToken : IRequest<RequestNewJobTokenResult>
    {
        public string Email { get; set; } = string.Empty;
    }

    internal class RequestNewJobTokenHandler : IRequestHandler<RequestNewJobToken, RequestNewJobTokenResult>
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

        public async Task<RequestNewJobTokenResult> Handle(RequestNewJobToken request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmail(request.Email);

            if (user == null || !user.IsVerified)
            {
                return RequestNewJobTokenResult.NoVerifiedUser;
            }

            var lastToken = await _tokenService.GetLastToken(user.Id, Token.TokenPurpose.CreateDraftJob);

            if (lastToken != null)
            {
                var wasRecent = (DateTime.UtcNow - lastToken.Created).TotalMinutes < 5;

                if (wasRecent)
                {
                    return RequestNewJobTokenResult.TooManyRequests;
                }
            }

            var token = await _tokenService.GenerateToken(user.Id, Token.TokenPurpose.CreateDraftJob, DateTime.UtcNow.AddDays(10));

            var message = $@"<p>Hi!</p><p>You requested a new job for PriceFalcon, use this link to create a new PriceFalcon monitoring job: 
                <a href='http://localhost:5220/create/new/{token.token}'>Get started</a>
                </p>";

            await _emailService.Send(user.Email, "Create a new job", message);

            return new RequestNewJobTokenResult(token.token);
        }
    }
}
