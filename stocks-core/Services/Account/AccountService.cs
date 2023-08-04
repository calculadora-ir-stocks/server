using Microsoft.Extensions.Logging;
using stocks.Exceptions;
using stocks.Notification;
using stocks.Repositories.Account;
using stocks_infrastructure.Models;

namespace stocks_core.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository repository;
        private readonly NotificationContext notificationContext;

        private readonly ILogger<AccountService> logger;

        public AccountService(IAccountRepository repository, NotificationContext notificationContext, ILogger<AccountService> logger)
        {
            this.repository = repository;
            this.notificationContext = notificationContext;
            this.logger = logger;
        }

        public void DeleteAccount(Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void UpdatePassword(Guid accountId, string password)
        {
            try
            {
                var account = repository.GetById(accountId);

                if (account is null) throw new NullReferenceException($"O usuário de id {accountId} não foi encontrado na base de dados.");
                if (account.Password == password) throw new InvalidBusinessRuleException("A nova senha não pode ser igual a senha atual.");

                // TODO: create helper to validate password
                account.Password = password;

                AccountValidator validator = new();
                var validatorResult = validator.Validate(account);

                if (validatorResult.Errors.Any())
                {
                    IEnumerable<string> messageErrors = validatorResult.Errors.Select(x => x.ErrorMessage);
                    notificationContext.AddNotifications(messageErrors);
                }

                account.HashPassword(password);
                repository.UpdatePassword(accountId, account);
            } catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao alterar a senha do usuário, {error}", e.Message);
                throw;
            }
        }
    }
}
