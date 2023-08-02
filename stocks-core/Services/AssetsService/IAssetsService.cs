using stocks_core.Models.Responses;
using stocks_core.Requests.BigBang;
using stocks_core.Responses;

namespace stocks.Services.IncomeTaxes;

public interface IAssetsService
{
    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês atual.
    /// </summary>
    Task<MonthTaxesResponse> GetCurrentMonthTaxes(Guid accountId);

    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês especificado.
    /// </summary>
    Task<MonthTaxesResponse> GetSpecifiedMonthTaxes(string month, Guid accountId);

    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no ano especificado.
    /// </summary>
    Task<IEnumerable<YearTaxesResponse>> GetSpecifiedYearTaxes(string year, Guid accountId);

    /// <summary>
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses desde 01/11/2019 até D-1.
    /// Também calcula e armazena o preço médio de todos os ativos.
    /// </summary>
    Task BigBang(Guid accountId, List<BigBangRequest> request);

}
