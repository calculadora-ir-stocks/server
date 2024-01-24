namespace Infrastructure.Repositories.Account
{
    public interface IAccountRepository
    {
        Task<bool> CPFExists(string cpf);
        Models.Account? GetById(Guid accountId);
        Models.Account GetByStripeCustomerId(string stripeCustomerId);
        IEnumerable<Models.Account> GetAll();
        void Delete(Models.Account account);
        void DeleteAll();
        void Update(Models.Account account);
        Task<Guid> GetByAuth0IdAsNoTracking(string auth0Id);

    }
}
