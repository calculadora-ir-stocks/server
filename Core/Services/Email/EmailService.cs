using Common.Models.Secrets;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Core.Services.Email
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class EmailService : IEmailService
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private const string EmailAddress = "contato@stocksir.app";
        private const string SenderName = "Stocks";

        private readonly SendGridSecret secret;

        public EmailService(IOptions<SendGridSecret> secret)
        {
            this.secret = secret.Value;
        }

        public async Task Send(string subject, string htmlContent)
        {
            SendGridClient client = new(secret.Token);

            var from = new EmailAddress(EmailAddress, SenderName);
            var to = new EmailAddress("account.Email", "account.Name");

            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode) throw new Exception();
        }
    }
}
