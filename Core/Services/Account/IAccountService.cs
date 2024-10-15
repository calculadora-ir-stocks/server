using Core.Models.Api.Requests.Account;

namespace Core.Services.Account
{
    public interface IAccountService
    {
        /// <summary>
        /// Deleta fisicamente o usuário da base e desvincula sua conta com a B3.
        /// </summary>
        /// <returns><c>true</c> se o desvínculo com a B3 for bem-sucedida; <c>false</c> caso contrário.</returns>
        Task<bool> Delete(Guid accountId);

        /// <summary>
        /// Diz se um usuário fez o opt-in com a B3.
        /// </summary>
        Task<bool> OptIn(Guid accountId);

        Task<Guid> GetByAuth0Id(string auth0Id);

        /// <returns>O link de opt-in da B3.</returns>
        string GetOptInLink();

        /// <summary>
        /// Configura o preço médio inicial (se algum) de uma conta cadastrada.
        /// </summary>
        Task SetupAverageTradedPrices(SetupAverageTradedPriceRequest request, Guid accountId);
    }
}
