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

        public Models.Account? GetByEmail(string email)
        {
            return _context.Accounts.AsEnumerable().SingleOrDefault(x => x.Email == email);
        }

        public void UpdatePassword(string currentPassword, string newPassword)
        {
            throw new NotImplementedException();
        }
    }
}
