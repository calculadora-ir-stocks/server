using Core.Models.B3;

namespace Api.Clients.B3
{
    public interface IB3Client
    {
        /// <summary>
        /// Dados referentes da movimentação de ações, fundos imobiliários, ouro e ETF.
        /// </summary>
        /// <returns></returns>
        Task<Movement.Root?> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId, string? nextUrl = null);
    }
}
