using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using Infrastructure.Repositories.EmailCode;
using Infrastructure.Repositories.Account;
using Stripe;
using Common.Helpers;

namespace Core.Services.Email
{
    public class EmailService : IEmailService
    {
        private const string StocksEmail = "calculadorastocks@gmail.com"; //TODO change it
        private const string StocksName = "Stocks";

        private readonly CustomerService stripeCustomerService;

        private readonly IEmailCodeRepository emailCodeRepository;
        private readonly IAccountRepository accountRepository;

        public EmailService(
            CustomerService stripeCustomerService,
            IEmailCodeRepository emailCodeRepository,
            IAccountRepository accountRepository
        )
        
        {
            this.stripeCustomerService = stripeCustomerService;
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
            var account = accountRepository.GetById(accountId)!;

            if (emailSender is null) throw new NotFoundException();

            if (emailSender.Code == code)
            {
                if (account.Status == EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.EmailNotConfirmed))
                {
                    // TODO unit of work
                    account.Status = EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.EmailConfirmed);

                    var customer = stripeCustomerService.Create(new CustomerCreateOptions
                    {
                        Name = account.Name,
                        Email = account.Email,
                        Phone = account.PhoneNumber
                    });

                    account.StripeCustomerId = customer.Id;

                    accountRepository.Update(account);
                }

                return true;
            }

            return false;
        }

        public async Task SendEmail(Infrastructure.Models.Account account, string verificationCode, string subject, string htmlContent)
        {
            try {
                // TODO criar variável de ambiente no Docker.
                string? apiKey = "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";

                SendGridClient client = new(apiKey);
                var from = new EmailAddress(StocksEmail, StocksName);

                var to = new EmailAddress(account.Email, account.Name);

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
