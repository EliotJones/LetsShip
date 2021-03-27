using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
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

    public enum EmailSendResult
    {
        Success = 1,
        Error = 2,
        InvalidRecipient = 3,
        ServiceUnavailable = 4,
        QuotaExceeded = 5
    }

    internal class SendGridEmailService : IEmailService
    {
        private readonly PriceFalconConfig _config;
        private readonly IEmailRepository _emailRepository;

        public SendGridEmailService(PriceFalconConfig config, IEmailRepository emailRepository)
        {
            _config = config;
            _emailRepository = emailRepository;
        }

        public async Task<EmailSendResult> Send(string recipient, string subject, string body)
        {
            var sentToday = await _emailRepository.GetSentTodayCount();

            if (sentToday >= 100)
            {
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
                await _emailRepository.Create(body, recipient, subject);

                return EmailSendResult.Success;
            }

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

            return response.IsSuccessStatusCode ? EmailSendResult.Success : EmailSendResult.Error;
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
