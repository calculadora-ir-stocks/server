namespace Infrastructure.Repositories.Account
{
    public interface IAccountRepository
    {
        bool EmailExists(string email);
        bool CPFExists(string cpf);
        Models.Account? GetByEmail(string email);
        Models.Account? GetById(Guid accountId);
        void UpdatePassword(Guid accountId, Models.Account account);
        IEnumerable<Models.Account> GetAll();
        IEnumerable<Models.Account> GetAllPremiums();
        void Delete(Models.Account account);
        void Update(Models.Account account);
    }
}
