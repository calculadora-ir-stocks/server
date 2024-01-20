namespace Core.Services.Account
{
    public interface IAccountService
    {
        /// <summary>
        /// Deleta fisicamente o usuário da base e desvincula sua conta com a B3.
        /// </summary>
        void Delete(Guid accountId);
    }
}
