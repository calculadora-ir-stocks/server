using Dapper;
using stocks.Database;

namespace stocks.Repositories.Account
{
    public class AccountRepository : IAccountRepository
    {

        private readonly StocksContext _context;

        public AccountRepository(StocksContext context)
        {
            _context = context;
        }

        public bool AccountExists(string email)
        {
            return _context.Accounts.Any(x => x.Email == email);
        }

        public void Delete(stocks_infrastructure.Models.Account account)
        {
            _context.Accounts.Remove(account);
            _context.SaveChanges();
        }

        public IEnumerable<stocks_infrastructure.Models.Account> GetAllAccounts()
        {
            return _context.Accounts.AsList();
        }

        public stocks_infrastructure.Models.Account? GetByEmail(string email)
        {
            return _context.Accounts.AsEnumerable().SingleOrDefault(x => x.Email == email);
        }

        public stocks_infrastructure.Models.Account? GetById(Guid accountId)
        {
            return _context.Accounts.Where(x => x.Id == accountId).FirstOrDefault();
        }

        public void UpdatePassword(Guid accountId, stocks_infrastructure.Models.Account account)
        {
            _context.Update(account);
            _context.SaveChanges();
        }
    }
}
