using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Registration
{
    public enum SendEmailInviteResult
    {
        Invalid = 0,
        Sent = 1,
        AlreadyVerified = 2,
        QuotaExceeded = 3
    }

    public class SendEmailInvite : IRequest<SendEmailInviteResult>
    {
        public string Email { get; }

        public SendEmailInvite(string email)
        {
            Email = email;
        }
    }

    internal class SendEmailInviteHandler : IRequestHandler<SendEmailInvite, SendEmailInviteResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly PriceFalconConfig _config;

        public SendEmailInviteHandler(IUserRepository userRepository, ITokenService tokenService, IEmailService emailService, PriceFalconConfig config)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        public async Task<SendEmailInviteResult> Handle(SendEmailInvite request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                return SendEmailInviteResult.Invalid;
            }

            var user = await _userRepository.GetByEmail(request.Email);

            if (user != null && user.IsVerified)
            {
                return SendEmailInviteResult.AlreadyVerified;
            }

            if (user == null)
            {
                user = await _userRepository.Create(request.Email);
            }
            else
            {
                var sent = await _emailService.GetAllSentToEmailInPeriod(request.Email, DateTime.UtcNow.AddHours(-5));

                if (sent.Count >= 3)
                {
                    return SendEmailInviteResult.QuotaExceeded;
                }
            }

            var token = await _tokenService.GenerateToken(user.Id, Token.TokenPurpose.ValidateEmail, DateTime.UtcNow.AddDays(10));

            var uri = new Uri(_config.SiteUrl, $"register/{token.token}");

            var message = $@"<p>Hi there,</p>
                <p>In order to begin using PriceFalcon you need to validate your email. Use the link below to validate your email and create a new watch job.</p>
                <p>If you didn't sign up you can safely ignore this email, sorry for the inconvenience.</p>
                <p><a href='{uri}'>Sign me up!</a></p>";
            
            await _emailService.Send(user.Email, "Verify your email", message);

            return SendEmailInviteResult.Sent;
        }
    }
}
