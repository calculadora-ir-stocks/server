using Api.Clients.B3;
using Common.Exceptions;
using Common.Options;
using Core.Models.B3;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Net.WebRequestMethods;

namespace Core.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository repository;
        private readonly IB3Client b3Client;
        private readonly IOptions<B3ApiOptions> b3Options;
        private readonly ILogger<AccountService> logger;

        public AccountService(IAccountRepository repository, IB3Client b3Client, IOptions<B3ApiOptions> b3Options, ILogger<AccountService> logger)
        {
            this.repository = repository;
            this.b3Client = b3Client;
            this.b3Options = b3Options;
            this.logger = logger;
        }

        public async Task Delete(Guid accountId)
        {
            try
            {
                var account = repository.GetById(accountId) ?? throw new NotFoundException("Investidor", accountId.ToString());

                await b3Client.OptOut(account.CPF);
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

        public string GetOptInLink()
        {
            string link = $"https://b3Investidorcer.b2clogin.com/b3Investidorcer.onmicrosoft.com/oauth2/v2.0/" +
                $"authorize?p=B2C_1A_FINTECH&client_id={b3Options.Value.ClientId}&nonce=defaultNonce" +
                $"&redirect_uri=https%3A%2F%2Fwww.investidor.b3.com.br&scope=openid&response_type=code&prompt=login";
            return link;
        }

        public async Task<bool> OptIn(string cpf)
        {
            return await b3Client.OptIn(cpf);
        }
    }
}
