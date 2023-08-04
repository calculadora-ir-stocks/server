using stocks_infrastructure.Models;

namespace stocks.Repositories.Account
{
    public interface IAccountRepository
    {
        bool AccountExists(string email);
        stocks_infrastructure.Models.Account? GetByEmail(string email);
        stocks_infrastructure.Models.Account? GetById(Guid accountId);
        void UpdatePassword(Guid accountId, stocks_infrastructure.Models.Account account);
        IEnumerable<stocks_infrastructure.Models.Account> GetAllAccounts();
    }
}
