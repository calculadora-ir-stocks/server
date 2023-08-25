using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using stocks.Repositories.Account;
using stocks_infrastructure.Repositories.EmailCode;

namespace stocks_core.Services.EmailSender
{
    public class EmailSenderService : IEmailSenderService
    {
        private const string StocksEmail = "calculadorastocks@gmail.com";
        private const string StocksName = "Stocks";

        private readonly IEmailCodeRepository emailCodeRepository;
        private readonly IAccountRepository accountRepository;

        public EmailSenderService(IEmailCodeRepository emailCodeRepository, IAccountRepository accountRepository)
        {
            this.emailCodeRepository = emailCodeRepository;
            this.accountRepository = accountRepository;
        }

        public bool CanSendEmailForUser(Guid accountId)
        {
            var emailSender = emailCodeRepository.GetByAccountId(accountId);

            // Verification e-mails are removed by Hangfire every 10 minutes.
            // If 10 minutes has passed, it is possible to send another e-mail for the specified user.
            if (emailSender is null) return true;

            return false;
        }

        public bool IsVerificationEmailValid(Guid accountId, string code)
        {
            var emailSender = emailCodeRepository.GetByAccountId(accountId);
            var account = accountRepository.GetById(accountId);

            if (emailSender is null) throw new NotFoundException();

            if (emailSender.Code == code)
            {
                if (account!.AuthenticationCodeValidated is false)
                {
                    account.AuthenticationCodeValidated = true;
                    accountRepository.Update(account);
                }

                return true;
            }

            return false;

        }

        public async Task SendEmail(stocks_infrastructure.Models.Account account, string verificationCode, string subject, string htmlContent)
        {
            try {
                // TODO: criar variável de ambiente no Docker.
                string? apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

                SendGridClient client = new(apiKey);
                var from = new EmailAddress(StocksEmail, StocksName);

                var to = new EmailAddress(account.CPF, account.Name);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode) throw new Exception();

                await emailCodeRepository.Create(verificationCode, account);
            } catch
            {
                throw;
            }
        }
    }
}
