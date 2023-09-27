using Api.Database;
using Dapper;

namespace Infrastructure.Repositories.Account
{
    public class AccountRepository : IAccountRepository
    {

        private readonly StocksContext context;

        public AccountRepository(StocksContext context)
        {
            this.context = context;
        }

        public bool EmailExists(string email)
        {
            return context.Accounts.Any(x => x.Email == email);
        }

        public bool CPFExists(string cpf)
        {
            return context.Accounts.Any(x => x.CPF == cpf);
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

        public Models.Account? GetByEmail(string email)
        {
            return context.Accounts.AsEnumerable().SingleOrDefault(x => x.Email == email);
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
    }
}
