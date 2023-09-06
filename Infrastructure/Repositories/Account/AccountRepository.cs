using Api.Database;
using Dapper;

namespace Infrastructure.Repositories.Account
{
    public class AccountRepository : IAccountRepository
    {

        private readonly StocksContext _context;

        public AccountRepository(StocksContext context)
        {
            _context = context;
        }

        public bool EmailExists(string email)
        {
            return _context.Accounts.Any(x => x.Email == email);
        }

        public bool CPFExists(string cpf)
        {
            return _context.Accounts.Any(x => x.CPF == cpf);
        }

        public void Delete(Models.Account account)
        {
            _context.Accounts.Remove(account);
            _context.SaveChanges();
        }

        public IEnumerable<Models.Account> GetAll()
        {
            return _context.Accounts.AsList();
        }

        public Models.Account? GetByEmail(string email)
        {
            return _context.Accounts.AsEnumerable().SingleOrDefault(x => x.Email == email);
        }

        public Models.Account? GetById(Guid accountId)
        {
            return _context.Accounts.Where(x => x.Id == accountId).FirstOrDefault();
        }

        public Models.Account GetByStripeCustomerId(string stripeCustomerId)
        {
            return _context.Accounts.Where(x => x.StripeCustomerId == stripeCustomerId).First();
        }

        public void Update(Models.Account account)
        {
            _context.Accounts.Update(account);
            _context.SaveChanges();
        }
    }
}
