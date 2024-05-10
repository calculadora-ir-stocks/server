using Api.Database;
using Common;
using Common.Constants;
using Common.Options;
using Dapper;
using Infrastructure.Models;
using Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Data.Common;

namespace Infrastructure.Repositories.Account
{
    public class AccountRepository : IAccountRepository
    {
        private readonly StocksContext context;
        private readonly IOptions<DatabaseEncryptionKeyOptions> key;
        private readonly IUnitOfWork unitOfWork;

        public AccountRepository(StocksContext context, IOptions<DatabaseEncryptionKeyOptions> key, IUnitOfWork unitOfWork)
        {
            this.context = context;
            this.key = key;
            this.unitOfWork = unitOfWork;
        }

        public async Task Create(Models.Account account)
        {
            DynamicParameters parameters = new();
            DbTransaction transaction = await unitOfWork.BeginTransactionAsync();

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);
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

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);
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

        public async Task<Models.Account?> GetById(Guid accountId)
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT 
                    a.""Id"",
                    a.""Auth0Id"",
                    PGP_SYM_DECRYPT(a.""CPF""::bytea, @Key) as CPF,
                    a.""BirthDate"",
                    a.""StripeCustomerId"",
                    a.""Status"",
                    a.""CreatedAt""
                FROM ""Accounts"" a
                WHERE a.""Id"" = @AccountId;";

            var account = await context.Database.GetDbConnection().QueryFirstOrDefaultAsync<Models.Account>(sql, parameters);

            Auditor.Audit($"{nameof(Account)}:{AuditOperation.Get}", null,
                comment: $"O objeto {nameof(Account)} foi obtido por inteiro pela aplicação e o CPF foi descriptografado.", fields: new { AccountId = accountId }
            );

            return account;
        }

        public Models.Account GetByStripeCustomerId(string stripeCustomerId)
        {
            return context.Accounts.Where(x => x.StripeCustomerId == stripeCustomerId).First();
        }

        public async Task UpdateStatus(Models.Account account)
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

            parameters.Add("@AccountId", account.Id);
            parameters.Add("@Status", account.Status);

            string sql = @"
                UPDATE
                    ""Accounts"" SET ""Status"" = @Status
                WHERE ""Id"" = @AccountId;";

            await context.Database.GetDbConnection().ExecuteAsync(sql, parameters);

            Auditor.Audit($"{nameof(Account)}:{AuditOperation.Update}", fields: new { NewStatus = account.Status });
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

        public async Task<IEnumerable<Models.Account>> GetAll()
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);

            string sql = @"
                SELECT 
                    a.""Id"",
                    a.""Auth0Id"",
                    PGP_SYM_DECRYPT(a.""CPF""::bytea, @Key) as CPF,
                    a.""BirthDate"",
                    a.""StripeCustomerId"",
                    a.""Status"",
                    a.""CreatedAt""
                FROM ""Accounts"" a;";

            var accounts = await context.Database.GetDbConnection().QueryAsync<Models.Account>(sql, parameters);

            Auditor.Audit($"{nameof(Account)}:{AuditOperation.Get}", null,
                comment: $"Todos os usuários foram obtidos da base com o CPF descriptografado."
            );

            return accounts;
        }
    }
}
