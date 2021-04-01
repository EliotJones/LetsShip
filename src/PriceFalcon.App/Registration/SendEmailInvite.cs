using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SendEmailInviteHandler> _logger;

        public SendEmailInviteHandler(
            IUserRepository userRepository,
            ITokenService tokenService,
            IEmailService emailService,
            PriceFalconConfig config,
            ILogger<SendEmailInviteHandler> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        public async Task<SendEmailInviteResult> Handle(SendEmailInvite request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Email invite requested for {request.Email}.");

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                return SendEmailInviteResult.Invalid;
            }

            var user = await _userRepository.GetByEmail(request.Email);

            if (user != null && user.IsVerified)
            {
                _logger.LogInformation($"Email {request.Email} has already been verified.");

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
                    _logger.LogInformation($"Email {request.Email} has been emailed {sent.Count} times in the past 5 hours, skipping invite.");

                    return SendEmailInviteResult.QuotaExceeded;
                }
            }

            var token = await _tokenService.GenerateToken(user.Id, Token.TokenPurpose.ValidateEmail, DateTime.UtcNow.AddDays(10));

            var uri = new Uri(_config.SiteUrl, $"register/{token.token}");

            var message = $@"<p>Hi there,</p>
                <p>In order to begin using PriceFalcon you need to validate your email. Use the link below to validate your email and create a new watch job.</p>
                <p>If you didn't sign up you can safely ignore this email, sorry for the inconvenience.</p>
                <p><a href='{uri}'>Sign me up!</a></p>";
            
            var result = await _emailService.Send(user.Email, "Verify your email", message);

            if (result == EmailSendResult.QuotaExceeded)
            {
                _logger.LogWarning($"Could not invite {user.Email} because the email quota was exceeded.");

                return SendEmailInviteResult.QuotaExceeded;
            }

            if (result == EmailSendResult.InvalidRecipient)
            {
                _logger.LogWarning($"Invalid recipient for email invite {user.Email}.");

                return SendEmailInviteResult.Invalid;
            }

            if (result == EmailSendResult.ServiceUnavailable || result == EmailSendResult.Error)
            {
                _logger.LogError($"Could not invite {user.Email} because the email service was down.");

                return SendEmailInviteResult.Invalid;
            }

            _logger.LogInformation($"Sent invite to {user.Email}.");

            return SendEmailInviteResult.Sent;
        }
    }
}
