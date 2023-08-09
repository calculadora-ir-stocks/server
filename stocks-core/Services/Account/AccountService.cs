using Microsoft.Extensions.Logging;
using stocks.Exceptions;
using stocks.Notification;
using stocks.Repositories.Account;
using stocks_core.Services.EmailSender;
using stocks_infrastructure.Models;

namespace stocks_core.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository repository;
        private readonly IEmailSenderService emailSenderService;

        private readonly NotificationContext notificationContext;

        private readonly ILogger<AccountService> logger;

        public AccountService(IAccountRepository repository, IEmailSenderService emailSenderService, NotificationContext notificationContext, ILogger<AccountService> logger)
        {
            this.repository = repository;
            this.emailSenderService = emailSenderService;
            this.notificationContext = notificationContext;
            this.logger = logger;
        }

        public void Delete(Guid accountId)
        {
            try
            {
                var account = repository.GetById(accountId);
                if (account is null) throw new NullReferenceException($"O usuário de id {accountId} não foi encontrado na base de dados.");

                repository.Delete(account);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao alterar a senha do usuário, {error}", e.Message);
                throw;
            }
        }

        public async Task SendEmailVerification(Guid accountId)
        {
            try
            {
                var account = repository.GetById(accountId);

                if (account is null) throw new InvalidBusinessRuleException($"O usuário de id {accountId} não foi encontrado em nossa base.");

                if (!emailSenderService.CanSendEmailForUser(accountId))
                    throw new InvalidBusinessRuleException($"O usuário de id {accountId} já enviou um código de verificação há pelo menos 10 minutos atrás.");

                // 4-digit random number
                string verificationCode = new Random().Next(1000, 9999).ToString();

                string Subject = "Confirme seu código de verificação";
                string HtmlContext = $"Olá, {account.Name}! O seu código de verificação é: <strong>{verificationCode}</strong>";

                await emailSenderService.SendEmail(account, verificationCode, Subject, HtmlContext);
            }
            catch
            {
                throw;
            }
        }

        public bool IsEmailVerificationCodeValid(Guid accountId, string code)
        {
            try
            {
                bool isValid = emailSenderService.IsVerificationEmailValid(accountId, code);
                return isValid;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao validar o código de validação do usuário {id}", accountId);
                throw;
            }
        }

        public void UpdatePassword(Guid accountId, string password)
        {
            try
            {
                var account = repository.GetById(accountId);
                if (account is null) throw new NullReferenceException($"O usuário de id {accountId} não foi encontrado na base de dados.");

                ValidateNewPassword(account, password);                

                account.Password = password;

                AccountValidator validator = new();
                var validatorResult = validator.Validate(account);

                if (validatorResult.Errors.Any())
                {
                    IEnumerable<string> messageError = validatorResult.Errors.Select(x => x.ErrorMessage);
                    notificationContext.AddNotifications(messageError);
                }

                account.HashPassword(password);
                repository.UpdatePassword(accountId, account);
            } catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao alterar a senha do usuário, {error}", e.Message);
                throw;
            }
        }


        private void ValidateNewPassword(stocks_infrastructure.Models.Account account, string password)
        {
            if (account.Password == password) throw new InvalidBusinessRuleException("A nova senha não pode ser igual à senha atual.");
        }
    }
}
