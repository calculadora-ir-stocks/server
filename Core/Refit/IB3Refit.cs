using Core.Models.B3;
using Refit;
using System.Net;

namespace Core.Refit.B3
{
    public interface IB3Refit
    {
        [Get("/acesso/healthcheck")]
        Task<HttpStatusCode> B3HealthCheck([Header("Bearer")] string token);

        /// <summary>
        /// Retorna as movimentações de um determinado investidor.
        /// </summary>
        /// <param name="cpf">Não deve conter caracteres especiais</param>
        /// <param name="referenceStartDate">Formato YYYY-MM-DD</param>
        /// <param name="referenceEndDate">Formato YYYY-MM-DD</param>
        [Get("/movement/v2/equities/investors/{cpf}?referenceStartDate={referenceStartDate}&referenceEndDate={referenceEndDate}")]
        Task<ApiResponse<Movement.Root?>> GetAccountMovements([Header("Bearer")] string token, string cpf, [Query] string referenceStartDate, [Query]  string referenceEndDate);

        /// <summary>
        /// Retorna as movimentações de um determinado investidor em uma determinada página.
        /// </summary>
        /// <param name="url">A URL retornada na propriedade <c>next</c> do retorno do endpoint <c>GetAccountMovements</c></param>
        [Get("/{url}")]
        Task<Movement.Root?> GetAccountMovementsByPage([Header("Bearer")] string token, string url);

        /// <summary>
        /// Consome o endpoint <c>/authorizations/investors/{documentNumber}</c> para validar se o <paramref name="cpf"/> realizou o opt-in anteriormente.
        /// </summary>
        /// <param name="cpf">Não deve conter caracteres especiais</param>
        [Get("/authorizations/investors/{cpf}")]
        Task<OptIn> OptIn([Header("Bearer")] string token, string cpf);

        /// <summary>
        /// Consome o endpoint <c>/optout/investor/{documentNumber}</c> para realizar o opt-out.
        /// </summary>
        /// <param name="cpf">Não deve conter caracteres especiais</param>
        [Get("/optout/investor/{cpf}")]
        Task OptOut([Header("Bearer")] string token, string cpf);
    }
}
