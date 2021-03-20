using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PriceFalcon.Infrastructure
{
    public interface IEmailService
    {
        Task<EmailSendResult> Send(string recipient, string subject, string body);
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

        public SendGridEmailService(PriceFalconConfig config)
        {
            _config = config;
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

            return response.IsSuccessStatusCode ? EmailSendResult.Success : EmailSendResult.Error;
        }
    }
}
