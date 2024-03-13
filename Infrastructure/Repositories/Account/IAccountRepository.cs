namespace Infrastructure.Repositories.Account
{
    public interface IAccountRepository
    {
        /// <summary>
        /// Insere um novo usuário na base com o CPF criptografado.
        /// </summary>
        Task Create(Models.Account account);
        Task<bool> CPFExists(string cpf);
        Models.Account? GetById(Guid accountId);
        Task<Guid> GetByAuth0IdAsNoTracking(string auth0Id);
        Models.Account GetByStripeCustomerId(string stripeCustomerId);
        IEnumerable<Models.Account> GetAll();
        void Delete(Models.Account account);
        void DeleteAll();
        void Update(Models.Account account);
    }
}
