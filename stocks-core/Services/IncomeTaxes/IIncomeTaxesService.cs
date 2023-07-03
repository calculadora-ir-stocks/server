using stocks_core.Models;
using stocks_core.Requests.BigBang;
using stocks_core.Requests.IncomeTaxes;

namespace stocks.Services.IncomeTaxes;

public interface IIncomeTaxesService
{
    /// <summary>
    /// Calcula a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês atual.
    /// </summary>
    Task<List<AssetIncomeTaxes>> CalculateCurrentMonthAssetsIncomeTaxes(AssetsIncomeTaxesRequest request);

    /// <summary>
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses desde 01/11/2019 até D-1.
    /// Também calcula e armazena o preço médio de todos os ativos.
    /// </summary>
    Task BigBang(Guid accountId, List<BigBangRequest> request);
}
