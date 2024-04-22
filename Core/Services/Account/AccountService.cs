using Common;
using Common.Constants;
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
                var account = repository.GetById(accountId) ?? 
                    throw new NullReferenceException($"O usuário de id {accountId} não foi encontrado na base de dados.");

                repository.Delete(account);

                logger.LogInformation("O usuário de id {accountId} deletou a sua conta da plataforma.", accountId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao deletar a conta do usuário de id {id}.", accountId);
                throw;
            }
        }

        public async Task<Guid> GetByAuth0Id(string auth0Id)
        {
            return await repository.GetByAuth0IdAsNoTracking(auth0Id);
        }
    }
}
