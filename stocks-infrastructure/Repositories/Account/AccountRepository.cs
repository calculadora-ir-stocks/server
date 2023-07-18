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

        /// <summary>
        /// Obtém todos os ids e seus respectivos CPFs.
        /// </summary>
        public IEnumerable<stocks_infrastructure.Models.Account> GetAllAccounts()
        {
            return _context.Accounts.AsList();
        }

        public stocks_infrastructure.Models.Account? GetByEmail(string email)
        {
            return _context.Accounts.AsEnumerable().SingleOrDefault(x => x.Email == email);
        }

        public void UpdatePassword(string currentPassword, string newPassword)
        {
            throw new NotImplementedException();
        }
    }
}
