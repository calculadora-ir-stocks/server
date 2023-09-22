using Core.Models.InfoSimples;
using Core.Models.Responses;
using Core.Requests.BigBang;
using Core.Responses;

namespace Core.Services.TaxesService;

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
    Task<TaxesDetailsResponse> GetTaxesByMonth(string month, Guid accountId);

    /// <summary>
    /// Retorna a quantidade de imposto de renda a ser pago para cada ativo de renda variável no ano especificado.
    /// </summary>
    Task<IEnumerable<CalendarResponse>> GetTaxesByYear(string year, Guid accountId);

    /// <summary>
    /// Faz o cálculo de impostos retroativos e preço médio de todos os ativos da conta de um investidor.
    /// Deve ser executado uma única vez quando um usuário cadastrar-se na plataforma.
    /// </summary>
    Task ExecuteB3Sync(Guid accountId, List<BigBangRequest> request);

    /// <summary>
    /// Marca o mês especificado como pago/não pago.
    /// </summary>
    Task SetAsPaidOrUnpaid(string month, Guid accountId);

    /// <summary>
    /// Cria uma DARF de um mês em que há imposto a ser pago para um determinado usuário.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="month"></param>
    Task<(GenerateDARFResponse, string)> GenerateDARF(Guid accountId, string month);
}
