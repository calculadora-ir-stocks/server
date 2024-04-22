using Api.Database;
using Common;
using Common.Configurations;
using Common.Constants;
using Dapper;
using Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Infrastructure.Repositories.Account
{
    public class AccountRepository : IAccountRepository
    {
        private readonly StocksContext context;
        private readonly AzureKeyVaultConfiguration keyVault;
        private readonly IUnitOfWork unitOfWork;

        public AccountRepository(StocksContext context, AzureKeyVaultConfiguration keyVault, IUnitOfWork unitOfWork)
        {
            this.context = context;
            this.keyVault = keyVault;
            this.unitOfWork = unitOfWork;
        }

        public async Task Create(Models.Account account)
        {
            DynamicParameters parameters = new();
            DbTransaction transaction = await unitOfWork.BeginTransactionAsync();

            var key = await keyVault.SecretClient.GetSecretAsync("pgcrypto-key");

            parameters.Add("@Key", key.Value.Value);
            parameters.Add("@AccountId", account.Id);
            parameters.Add("@Auth0Id", account.Auth0Id);
            parameters.Add("@CPF", account.CPF);
            parameters.Add("@BirthDate", account.BirthDate);
            parameters.Add("@StripeCustomerId", account.StripeCustomerId);
            parameters.Add("@Status", account.Status);

            string createAccount = @"
                INSERT INTO ""Accounts""
                    (""Id"", ""Auth0Id"", ""CPF"", ""BirthDate"", ""StripeCustomerId"", ""Status"", ""CreatedAt"")
                VALUES
                    (@AccountId, @Auth0Id, PGP_SYM_ENCRYPT(@CPF, @Key), @BirthDate, @StripeCustomerId, @Status, NOW());
            ";

            string createPlan = @"
                INSERT INTO ""Plans""
                    (""Id"", ""Name"", ""AccountId"", ""ExpiresAt"", ""CreatedAt"") 
                VALUES
                    (gen_random_uuid(), 'Gratuito', @AccountId, 'NOW()'::timestamp + '1 month'::INTERVAL, NOW());
            ";

            await transaction.Connection.QueryAsync(createAccount, parameters);
            await transaction.Connection.QueryAsync(createPlan, parameters);
            await transaction.CommitAsync();

            Auditor.Audit($"{nameof(Account)}:{AuditOperation.Add}", comment: "O CPF do usuário foi criptografado na base de dados.");
        }

        public async Task<bool> CPFExists(string cpf, Guid accountId)
        {
            DynamicParameters parameters = new();

            var key = await keyVault.SecretClient.GetSecretAsync("pgcrypto-key");

            parameters.Add("@Key", key.Value.Value);
            parameters.Add("@CPF", cpf);

            string sql = @"
                SELECT 
                    a.""CPF"" FROM ""Accounts"" a
                WHERE
                    PGP_SYM_DECRYPT(a.""CPF""::bytea, @Key) = @CPF
            ";

            string encryptedCPF = await context.Database.GetDbConnection().QuerySingleOrDefaultAsync<string>(sql, parameters);

            Auditor.Audit($"{nameof(Account)}:{AuditOperation.Get}", null,
                comment: $"O CPF criptografado do usuário foi descriptografado a nível de banco e processado pela aplicação para verificar se o CPF já está " +
                "cadastrado na plataforma.", fields: new { CPF = encryptedCPF, AccountId = accountId }
            );

            return encryptedCPF is not null;
        }

        public void Delete(Models.Account account)
        {
            context.Accounts.Remove(account);
            context.SaveChanges();
            Auditor.Audit($"{nameof(Models.Account)}:{AuditOperation.Delete}", fields: new { AccountId = account.Id });
        }

        public IEnumerable<Models.Account> GetAll()
        {
            return context.Accounts.AsList();
        }

        public Models.Account? GetById(Guid accountId)
        {
            return context.Accounts.Where(x => x.Id == accountId).FirstOrDefault();
        }

        public Models.Account GetByStripeCustomerId(string stripeCustomerId)
        {
            return context.Accounts.Where(x => x.StripeCustomerId == stripeCustomerId).First();
        }

        public void Update(Models.Account account)
        {
            context.Accounts.Update(account);
            context.SaveChanges();
        }

        public void DeleteAll()
        {
            context.Accounts.RemoveRange(context.Accounts);
            context.SaveChanges();
        }

        public Task<Guid> GetByAuth0IdAsNoTracking(string auth0Id)
        {
            return context.Accounts.AsNoTracking().Where(x => x.Auth0Id.Equals(auth0Id)).Select(x => x.Id).FirstOrDefaultAsync();
        }
    }
}
