using Microsoft.Extensions.Logging;
using Api.Exceptions;
using Api.Notification;
using Infrastructure.Repositories.Account;
using Infrastructure.Models;
using Core.Services.Email;
using Common.Exceptions;
using Common.Helpers;

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
            try
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
                if (!emailSenderService.CanSendEmailForUser(accountId))
                    throw new BadRequestException($"O usuário de id {accountId} já enviou um código de verificação há pelo menos 10 minutos atrás.");

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
                repository.Update(account);
            } catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao alterar a senha do usuário, {error}", e.Message);
                throw;
            }
        }

        private void ValidateNewPassword(Infrastructure.Models.Account account, string password)
        {
            if (account.Password == password) throw new BadRequestException("A nova senha não pode ser igual à senha atual.");
        }

        public bool IsSynced(Guid accountId)
        {
            var account = repository.GetById(accountId);

            if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

            if (account.Status == EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.EmailNotConfirmed) ||
                account.Status == EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.EmailConfirmed))
            {
                throw new BadRequestException("O Big Bang ainda não foi executado para esse usuário.");
            }

            return account.Status != EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.Syncing);
        }
    }
}
