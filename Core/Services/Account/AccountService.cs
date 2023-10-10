using Api.Notification;
using Common.Exceptions;
using Common.Helpers;
using Core.Services.Email;
using DevOne.Security.Cryptography.BCrypt;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;

namespace Core.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository repository;
        private readonly IEmailService emailSenderService;

        private readonly NotificationContext notificationContext;

        private readonly ILogger<AccountService> logger;

        public AccountService(
            IAccountRepository repository,
            IEmailService emailSenderService,
            NotificationContext notificationContext,
            ILogger<AccountService> logger
        )
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

        public async Task SendEmailVerification(Guid accountId, Infrastructure.Models.Account? account = null)
        {
            account ??= repository.GetById(accountId);

            if (account is null) throw new BadRequestException($"O usuário de id {accountId} não foi encontrado em nossa base.");

            if (!emailSenderService.CanSendEmailForUser(accountId))
                throw new BadRequestException($"O usuário de id {accountId} já enviou um código de verificação há pelo menos 10 minutos atrás.");

            // 4-digit random number
            string verificationCode = new Random().Next(1000, 9999).ToString();

            string Subject = "Confirme seu código de verificação";
            string HtmlContext = $"Olá {account.Name}, que surpresa agradável! O seu código de verificação é: <strong>{verificationCode}</strong>";

            await emailSenderService.SendEmail(account, verificationCode, Subject, HtmlContext);
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
                if (!emailSenderService.CanSendEmailForUser(accountId))
                    throw new BadRequestException($"O usuário de id {accountId} já enviou um código de verificação há pelo menos 10 minutos atrás.");

                var account = repository.GetById(accountId);
                if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

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
                repository.Update(account);
            } catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao alterar a senha do usuário, {error}", e.Message);
                throw;
            }
        }

        private void ValidateNewPassword(Infrastructure.Models.Account account, string password)
        {
            if (BCryptHelper.CheckPassword(password, account.Password)) throw new BadRequestException("A nova senha não pode ser igual à senha atual.");
        }

        public async Task<Guid> ForgotPassword(string email)
        {
            var account = repository.GetByEmail(email);
            if (account is null) throw new NotFoundException("Investidor com o e-mail informado não encontrado.");

            await SendEmailVerification(account.Id, account);

            return account.Id;
        }
    }
}
