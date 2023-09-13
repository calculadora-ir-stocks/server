using Core.Requests.BigBang;
using Core.Services.TaxesService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
// [Authorize]
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
    /// <param name="month">O mês onde há impostos a ser pago que a DARF será gerada</param>
    /// <returns></returns>
    [HttpGet("generate-darf")]
    public async Task<IActionResult> GenerateDarf(Guid accountId, string month)
    {
        await service.GenerateDARF(accountId, month);
        return Ok();
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos do mês atual.
    /// </summary>
    [HttpGet("current/{accountId}")]
    public async Task<IActionResult> GetCurrentMonthTaxes(Guid accountId)
    {
        var response = await service.GetCurrentMonthTaxes(accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no mês especificado.
    /// </summary>
    /// <param name="month">Formato: MM/yyyy</param>
    /// <param name="accountId"></param>
    /// <returns></returns>
    [HttpGet("month/{month}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedMonthTaxes(string month, Guid accountId)
    {
        var response = await service.GetTaxesByMonth(month, accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o mês especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no ano especificado.
    /// </summary>
    /// <param name="year">Formato: yyyy</param>
    /// <param name="accountId"></param>
    /// <returns></returns>
    [HttpGet("year/{year}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedYearTaxes(string year, Guid accountId)
    {
        var response = await service.GetTaxesByYear(year, accountId);

        if (response.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o ano especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Altera o mês especificado como pago/não pago.
    /// </summary>
    /// <param name="month">Formato: MM/yyyy</param>
    /// <param name="accountId"></param>
    /// <returns></returns>
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
