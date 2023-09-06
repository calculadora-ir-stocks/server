namespace Infrastructure.Repositories.EmailCode
{
    public interface IEmailCodeRepository
    {
        Models.EmailCode? GetByAccountId(Guid accountId);
        Task Create(string code, Models.Account account);
        void Delete(Models.EmailCode emailSender);
        IEnumerable<Models.EmailCode> GetAll();
    }
}
