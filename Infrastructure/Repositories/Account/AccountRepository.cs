using Api.Database;
using Dapper;
using Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Infrastructure.Repositories.Account
{
    public class AccountRepository : IAccountRepository
    {
        private readonly StocksContext context;
        private readonly IUnitOfWork unitOfWork;

        public AccountRepository(StocksContext context, IUnitOfWork unitOfWork)
        {
            this.context = context;
            this.unitOfWork = unitOfWork;
        }

        public async Task Create(Models.Account account)
        {
            DynamicParameters parameters = new();
            DbTransaction transaction = await unitOfWork.BeginTransactionAsync();

            const string key = "GET THIS SHIT FROM A HSM";
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
        }

        public async Task<bool> CPFExists(string cpf)
        {
            return await context.Accounts.AnyAsync(x => x.CPF == cpf);
        }

        public void Delete(Models.Account account)
        {
            context.Accounts.Remove(account);
            context.SaveChanges();
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
