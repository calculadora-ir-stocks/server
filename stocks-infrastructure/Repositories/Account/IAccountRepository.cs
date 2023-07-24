namespace stocks.Repositories.Account
{
    public interface IAccountRepository
    {
        bool AccountExists(string email);
        stocks_infrastructure.Models.Account? GetByEmail(string email);
        void UpdatePassword(string currentPassword, string newPassword);
        IEnumerable<stocks_infrastructure.Models.Account> GetAllAccounts();
    }
}
