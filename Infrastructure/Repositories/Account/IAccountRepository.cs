namespace Infrastructure.Repositories.Account
{
    public interface IAccountRepository
    {
        bool EmailExists(string email);
        bool CPFExists(string cpf);
        Models.Account? GetByEmail(string email);
        Models.Account? GetById(Guid accountId);
        public Models.Account GetByStripeCustomerId(string stripeCustomerId);
        IEnumerable<Models.Account> GetAll();
        IEnumerable<Models.Account> GetAllPremiums();
        void Delete(Models.Account account);
        void Update(Models.Account account);
    }
}
