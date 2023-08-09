using stocks_infrastructure.Models;

namespace stocks_infrastructure.Repositories.EmailCode
{
    public interface IEmailCodeRepository
    {
        Models.EmailCode? GetByAccountId(Guid accountId);
        Task Create(string code, Account account);
        void Delete(Models.EmailCode emailSender);
        IEnumerable<Models.EmailCode> GetAll();
    }
}
