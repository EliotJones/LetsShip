using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure.DataAccess;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PriceFalcon.Infrastructure
{
    public interface IEmailService
    {
        Task<EmailSendResult> Send(string recipient, string subject, string body);

        Task<IReadOnlyList<Email>> GetAllSent();

        Task<IReadOnlyList<Email>> GetAllSentToEmailInPeriod(string recipient, DateTime fromInclusive);

        Task<int> GetSentTodayCount();
    }

    internal class SendGridEmailService : IEmailService
    {
        private readonly PriceFalconConfig _config;
        private readonly IEmailRepository _emailRepository;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(PriceFalconConfig config, IEmailRepository emailRepository, ILogger<SendGridEmailService> logger)
        {
            _config = config;
            _emailRepository = emailRepository;
            _logger = logger;
        }

        public async Task<EmailSendResult> Send(string recipient, string subject, string body)
        {
            var sentToday = await _emailRepository.GetSentTodayCount();

            if (sentToday >= 100)
            {
                _logger.LogInformation($"Could not send an email '{subject}' to {recipient} because we've already sent {sentToday} emails today.");

                return EmailSendResult.QuotaExceeded;
            }

            var key = _config.SendGridApiKey;

            var client = new SendGridClient(key);
            var from = new EmailAddress("noreply.pricefalcon@pricefalcon.com");
            var to = new EmailAddress(recipient);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);
            msg.MailSettings = new MailSettings
            {
                SandboxMode = new SandboxMode
                {
                    Enable = true
                }
            };

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Successfully sent email '{subject}' to {recipient} with status {response.StatusCode}.");

                await _emailRepository.Create(body, recipient, subject);

                return EmailSendResult.Success;
            }

            var content = await response.Body.ReadAsStringAsync();

            _logger.LogError($"Failed to send email '{subject}' to {recipient}. SendGrid responded with status {response.StatusCode}: {content}.");

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return EmailSendResult.QuotaExceeded;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable
                || response.StatusCode == HttpStatusCode.BadGateway
                || response.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                return EmailSendResult.ServiceUnavailable;
            }

            return EmailSendResult.Error;
        }

        public async Task<IReadOnlyList<Email>> GetAllSent()
        {
            return await _emailRepository.GetAll();
        }

        public async Task<IReadOnlyList<Email>> GetAllSentToEmailInPeriod(string recipient, DateTime fromInclusive)
        {
            if (fromInclusive > DateTime.UtcNow || string.IsNullOrWhiteSpace(recipient))
            {
                return Array.Empty<Email>();
            }

            return await _emailRepository.GetAllSentToEmailInPeriod(recipient, fromInclusive);
        }

        public Task<int> GetSentTodayCount()
        {
            return _emailRepository.GetSentTodayCount();
        }
    }
}
