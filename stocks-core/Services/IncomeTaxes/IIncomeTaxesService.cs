using stocks_core.Requests.BigBang;
using stocks_core.Responses;

namespace stocks.Services.IncomeTaxes;

public interface IIncomeTaxesService
{
    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês atual.
    /// </summary>
    Task<MonthTaxesResponse> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId);

    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês especificado.
    /// </summary>
    Task<MonthTaxesResponse> CalculateSpecifiedMonthAssetsIncomeTaxes(string month, Guid accountId);

    /// <summary>
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses desde 01/11/2019 até D-1.
    /// Também calcula e armazena o preço médio de todos os ativos.
    /// </summary>
    Task BigBang(Guid accountId, List<BigBangRequest> request);
}
