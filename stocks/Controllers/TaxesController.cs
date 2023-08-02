using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using stocks.Services.IncomeTaxes;
using stocks_core.Requests.BigBang;

namespace stocks.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Tags("Income taxes")]
public class TaxesController : BaseController
{
    private readonly IAssetsService service;

    public TaxesController(IAssetsService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos em renda variável do mês atual.
    /// </summary>
    [HttpGet("assets/current/{accountId}")]
    public async Task<IActionResult> GetCurrentMonthTaxes(Guid accountId)
    {
        var response = await service.GetCurrentMonthTaxes(accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        // TO-DO: alterar AssetId pelo nome do ativo.
        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos em renda variável no mês especificado.
    /// Formato: MM-yyyy
    /// </summary>
    [HttpGet("assets/month/{month}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedMonthTaxes(string month, Guid accountId)
    {
        var response = await service.GetSpecifiedMonthTaxes(month, accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o mês especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos em renda variável no ano especificado.
    /// Formato: yyyy
    /// </summary>
    [HttpGet("assets/year/{year}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedYearTaxes(string year, Guid accountId)
    {
        var response = await service.GetSpecifiedYearTaxes(year, accountId);

        if (response.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o ano especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Altera o mês especificado como pago/não pago.\n
    /// Formato: MM-yyyy
    /// </summary>
    [HttpPut("assets/set-paid-or-unpaid/{month}/{accountId}")]
    public async Task<IActionResult> SetMonthAsPaid(string month, Guid accountId)
    {
        await service.SetMonthAsPaidOrUnpaid(month, accountId);
        return Ok("O mês especificado foi alterado para pago/não pago com sucesso.");
    }

    /// <summary>
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses retroativos.
    /// Também calcula e armazena o preço médio de todos os ativos até a data atual.
    /// </summary>
    [HttpPost("big-bang/{id}")]
    public async Task<IActionResult> BigBang(Guid id, [FromBody] List<BigBangRequest> request)
    {
        await service.BigBang(id, request);
        return Ok("Imposto de renda e preço médio mais recente calculados e armazenados com sucesso.");
    }

    /// <summary>
    /// Calcula o imposto de renda de criptomoedas.
    /// </summary>
    [HttpPost("cryptocurrency")]
    public IActionResult CalculateCryptocurrency()
    {
        return Ok();
    }

    /// <summary>
    /// Calcula o imposto de renda de NFTs (kkkkk).
    /// </summary>
    [HttpPost("nfts")]
    public IActionResult CalculateNFTs()
    {
        return Ok();
    }
}
