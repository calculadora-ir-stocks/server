using Core.Models.Responses;

namespace Core.Services.DarfGenerator
{
    public interface IDarfGeneratorService
    {
        /// <summary>
        /// Gera uma DARF através do site <a href="https://sicalc.receita.economia.gov.br/sicalc/principal">sicalc</a>.
        /// </summary>
        /// <param name="accountId">O id do usuário.</param>
        /// <param name="month">O mês em questão para gerar a DARF. Formato MM/yyyy</param>
        /// <param name="value">Valor a compensar para adicionar no valor total da DARF.</param>
        Task<DARFResponse> Generate(Guid accountId, string month, double value = 0);
    }
}
