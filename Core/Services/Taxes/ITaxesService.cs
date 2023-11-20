using Core.Models.Api.Responses;
using Core.Models.Responses;

namespace Core.Services.Taxes;

/// <summary>
/// Especifica um contrato com todas as funcionalidades envolvendo impostos.
/// </summary>
public interface ITaxesService
{
    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês atual.
    /// </summary>
    Task<TaxesDetailsResponse> GetCurrentMonthTaxes(Guid accountId);

    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no mês especificado.
    /// </summary>
    Task<TaxesDetailsResponse> Details(string month, Guid accountId);

    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no ano especificado.
    /// </summary>
    Task<IEnumerable<CalendarResponse>> GetCalendarTaxes(string year, Guid accountId);

    /// <summary>
    /// Marca o mês especificado como pago/não pago.
    /// </summary>
    Task SetAsPaidOrUnpaid(string month, Guid accountId);
}
