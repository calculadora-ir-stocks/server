using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using stocks.Services.IncomeTaxes;
using stocks_core.Requests.BigBang;

namespace stocks.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Tags("Taxes")]
// TODO [Authorize]
public class TaxesController : BaseController
{
    private readonly IAssetsService service;

    public TaxesController(IAssetsService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos do mês atual.
    /// </summary>
    [HttpGet("current/{accountId}")]
    public async Task<IActionResult> GetCurrentMonthTaxes(Guid accountId)
    {
        var response = await service.GetCurrentMonthTaxes(accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        // TO-DO: alterar AssetId pelo nome do ativo.
        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no mês especificado.
    /// Formato: MM-yyyy
    /// </summary>
    [HttpGet("{month}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedMonthTaxes(string month, Guid accountId)
    {
        var response = await service.GetSpecifiedMonthTaxes(month, accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o mês especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas os meses em que há imposto a ser pago no ano especificado.
    /// Formato: yyyy
    /// </summary>
    [HttpGet("{year}/{accountId}")]
    public async Task<IActionResult> GetSpecifiedYearTaxes(string year, Guid accountId)
    {
        var response = await service.GetSpecifiedYearTaxes(year, accountId);

        if (response.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o ano especificado.");

        return Ok(response);
    }

    /// <summary>
    /// Altera o mês especificado como pago/não pago.
    /// Formato: MM-yyyy
    /// </summary>
    [HttpPut("set-paid-or-unpaid/{month}/{accountId}")]
    public async Task<IActionResult> SetMonthAsPaid(string month, Guid accountId)
    {
        await service.SetMonthAsPaidOrUnpaid(month, accountId);
        return Ok("O mês especificado foi alterado para pago/não pago com sucesso.");
    }

    /// <summary>
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses retroativos.
    /// Também calcula e armazena o preço médio de todos os ativos até a data atual.
    /// </summary>
    [HttpPost("big-bang/{accountId}")]
    public async Task<IActionResult> BigBang(Guid accountId, [FromBody] List<BigBangRequest> request)
    { 
        await service.BigBang(accountId, request);
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
