using System.Collections.Generic;
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
    }

    public enum EmailSendResult
    {
        Success = 1,
        Error = 2,
        InvalidRecipient = 3,
        ServiceUnavailable = 4
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
            }

            return response.IsSuccessStatusCode ? EmailSendResult.Success : EmailSendResult.Error;
        }

        public async Task<IReadOnlyList<Email>> GetAllSent()
        {
            return await _emailRepository.GetAll();
        }
    }
}
