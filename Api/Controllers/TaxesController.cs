using Core.Requests.BigBang;
using Core.Services.TaxesService;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Authorize]
[Tags("Taxes")]
public class TaxesController : BaseController
{
    private readonly ITaxesService service;

    public TaxesController(ITaxesService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Gera uma DARF para o usuário especificado referente a um mês onde há impostos a ser pago.
    /// </summary>
    /// <param name="accountId">O id do usuário</param>
    /// <param name="month">O mês onde há impostos a ser pago que a DARF será gerada. Formato: MM/yyyy</param>
    /// <returns>O código de barras da DARF e outras informações referentes ao imposto sendo pago.</returns>
    [HttpGet("generate-darf")]
    public async Task<IActionResult> GenerateDarf(Guid accountId, string month)
    {
        var response = await service.GenerateDARF(accountId, month);

        return Ok(new
        {
            barCode = response.Item1.Data[0].CodigoDeBarras,
            interest = response.Item1.Data[0].Totais.Juros,
            fine = response.Item1.Data[0].Totais.Multa,
            total = response.Item1.Data[0].Totais.NormalizadoTotal,
            comments = response.Item2
        });
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos do mês atual.
    /// </summary>
    [HttpGet("home/{accountId}")]
    public async Task<IActionResult> GetCurrentMonthTaxes(Guid accountId)
    {
        var response = await service.GetCurrentMonthTaxes(accountId);

        if (response.Movements.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no mês especificado.
    /// </summary>
    /// <param name="month">Formato: MM/yyyy</param>
    /// <param name="accountId"></param>
    [HttpGet("details/{month}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedMonthTaxes(string month, Guid accountId)
    {
        var response = await service.GetTaxesByMonth(month, accountId);

        if (response.Movements.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o mês especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no ano especificado.
    /// </summary>
    /// <param name="year">Formato: yyyy</param>
    /// <param name="accountId"></param>
    [HttpGet("calendar/{year}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedYearTaxes(string year, Guid accountId)
    {
        var response = await service.GetCalendarTaxes(year, accountId);

        if (response.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o ano especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Altera o mês especificado como pago/não pago.
    /// </summary>
    /// <param name="month">Formato: MM/yyyy</param>
    /// <param name="accountId"></param>
    [HttpPut("set-paid-or-unpaid/{month}/{accountId}")]
    public async Task<IActionResult> SetMonthAsPaid(string month, Guid accountId)
    {
        await service.SetAsPaidOrUnpaid(month, accountId);
        return Ok(new { message = "O mês especificado foi alterado para pago/não pago com sucesso." });
    }

    /// <summary>
    /// Faz o cálculo de impostos retroativos e preço médio de todos os ativos da conta de um investidor.
    /// Deve ser executado uma única vez quando um usuário cadastrar-se na plataforma.
    /// </summary>
    [HttpPost("big-bang/{accountId}")]
    public async Task<IActionResult> BigBang(Guid accountId, [FromBody] List<BigBangRequest> request)
    { 
        await service.ExecuteB3Sync(accountId, request);
        return Ok(new { message = "Imposto de renda e preço médio mais recente calculados e armazenados com sucesso." });
    }
}
