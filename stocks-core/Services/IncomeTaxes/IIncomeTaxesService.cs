using stocks_core.Response;

namespace stocks.Services.IncomeTaxes;

public interface IIncomeTaxesService
{
    /// <summary>
    /// Calcula a quantidade de imposto de renda a ser pago para cada ativo de renda variável.
    /// </summary>
    Task<CalculateAssetsIncomeTaxesResponse?> CalculateAssetsIncomeTaxes(Guid accountId);
}
