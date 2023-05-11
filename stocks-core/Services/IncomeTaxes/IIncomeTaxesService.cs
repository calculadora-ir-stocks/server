using stocks.Requests;
using stocks_core.Response;

namespace stocks.Services.IncomeTaxes;

public interface IIncomeTaxesService
{
    /// <summary>
    /// Calcula a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês atual.
    /// </summary>
    Task<AssetIncomeTaxes?> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId);

    /// <summary>
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses desde 01/11/2019 até D-1.
    /// Também calcula e armazena o preço médio de todos os ativos.
    /// </summary>
    Task CalculateIncomeTaxesForEveryMonth(Guid accountId, List<CalculateIncomeTaxesForEveryMonthRequest> request);
}
