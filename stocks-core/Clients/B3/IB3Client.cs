using stocks_core.DTOs.B3;

namespace stocks.Clients.B3
{
    public interface IB3Client
    {
        /// <summary>
        /// Dados referentes da movimentação de ações, fundos imobiliários, ouro e ETF.
        /// </summary>
        /// <returns></returns>
        Task<Movement.Root> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId, string? nextUrl = null);

        /// <summary>
        /// Dados referentes a todos os ativos na conta do investidor.
        /// </summary>
        /// <returns></returns>
        Task<Position.Root> GetAccountPosition(string cpf, string referenceDate, string? nextUrl = null);
    }
}
