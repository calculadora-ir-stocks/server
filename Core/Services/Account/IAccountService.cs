namespace Core.Services.Account
{
    public interface IAccountService
    {
        /// <summary>
        /// Deleta fisicamente o usuário da base e desvincula sua conta com a B3.
        /// </summary>
        Task Delete(Guid accountId);

        /// <summary>
        /// Diz se um usuário fez o opt-in com a B3.
        /// </summary>
        Task<bool> OptIn(string cpf);

        Task<Guid> GetByAuth0Id(string auth0Id);

        /// <returns>O link de opt-in da B3.</returns>
        string GetOptInLink();
    }
}
