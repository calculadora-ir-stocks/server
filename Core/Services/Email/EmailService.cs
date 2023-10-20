using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using Infrastructure.Repositories.EmailCode;
using Infrastructure.Repositories.Account;
using Stripe;
using Common.Helpers;
using Common.Models;
using Microsoft.Extensions.Options;

namespace Core.Services.Email
{
    public class EmailService : IEmailService
    {
        private const string EmailAddress = "contato@stocksir.app";
        private const string SenderName = "Stocks";

        private readonly CustomerService stripeCustomerService;

        private readonly IEmailCodeRepository emailCodeRepository;
        private readonly IAccountRepository accountRepository;

        private readonly SendGridSecret secret;

        public EmailService(
            CustomerService stripeCustomerService,
            IEmailCodeRepository emailCodeRepository,
            IAccountRepository accountRepository,
            IOptions<SendGridSecret> secret
        )
        
        {
            this.stripeCustomerService = stripeCustomerService;
            this.emailCodeRepository = emailCodeRepository;
            this.accountRepository = accountRepository;
            this.secret = secret.Value;
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
            var emailCode = emailCodeRepository.GetByAccountId(accountId);
            var account = accountRepository.GetById(accountId)!;

            if (emailCode is null) throw new NotFoundException();

            if (emailCode.Code == code)
            {
                if (account.Status == EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.EmailNotConfirmed))
                {
                    account.Status = EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.EmailConfirmed);

                    var customer = stripeCustomerService.Create(new CustomerCreateOptions
                    {
                        Name = account.Name,
                        Email = account.Email,
                        Phone = account.PhoneNumber
                    });

                    account.StripeCustomerId = customer.Id;

                    accountRepository.Update(account);
                    emailCodeRepository.Delete(emailCode);
                }

                return true;
            }

            return false;
        }

        public async Task SendEmail(Infrastructure.Models.Account account, string verificationCode, string subject, string htmlContent)
        {
            SendGridClient client = new(secret.Token);

            var from = new EmailAddress(EmailAddress, SenderName);

            var to = new EmailAddress(account.Email, account.Name);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode) throw new Exception();

            await emailCodeRepository.Create(verificationCode, account);
        }
    }
}
