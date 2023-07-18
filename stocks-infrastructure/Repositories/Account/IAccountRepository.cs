using stocks.Models;

namespace stocks.Repositories.Account
{
    public interface IAccountRepository
    {
        bool AccountExists(string email);
        Models.Account? GetByEmail(string email);
        void UpdatePassword(string currentPassword, string newPassword);
        IEnumerable<(Guid, string)> GetAllIdsAndCpf();
    }
}
