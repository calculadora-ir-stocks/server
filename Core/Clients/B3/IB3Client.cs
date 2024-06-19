using Core.Models.B3;
using System.Net;

namespace Api.Clients.B3
{
    public interface IB3Client
    {
        Task<HttpStatusCode> B3HealthCheck();
        /// <summary>
        /// Dados referentes a movimentação de ações, fundos imobiliários, ouro e ETF.
        /// </summary>
        Task<Movement.Root?> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId, string? nextUrl = null);

        /// <summary>
        /// Consome o endpoint <c>/authorizations/investors/{documentNumber}</c> para validar se o <paramref name="cpf"/> realizou o opt-in anteriormente.
        /// </summary>
        Task<bool> OptIn(string cpf);

        /// <summary>
        /// Consome o endpoint <c>/optout/investor/{documentNumber}</c> para realizar o opt-out.
        /// </summary>
        Task OptOut(string cpf);
    }
}
