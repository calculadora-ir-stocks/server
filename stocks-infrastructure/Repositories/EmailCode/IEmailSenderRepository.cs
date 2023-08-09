using stocks_infrastructure.Models;

namespace stocks_infrastructure.Repositories.EmailCode
{
    public interface IEmailSenderRepository
    {
        Models.EmailCode? GetByAccountId(Guid accountId);
        Task Create(string code, Account account);
        void Delete(Models.EmailCode emailSender);
    }
}
