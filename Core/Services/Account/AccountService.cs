using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;

namespace Core.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository repository;
        private readonly ILogger<AccountService> logger;

        public AccountService(IAccountRepository repository, ILogger<AccountService> logger)
        {
            this.repository = repository;
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
    }
}
