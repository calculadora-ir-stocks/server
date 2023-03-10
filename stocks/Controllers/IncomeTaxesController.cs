using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stocks.Services.IncomeTaxes;

namespace stocks.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Tags("Income taxes")]
public class IncomeTaxesController : BaseController
{
    private readonly IIncomeTaxesService _service;

    public IncomeTaxesController(IIncomeTaxesService service)
    {
        _service = service;
    }

    /// <summary>
    /// Calcula o total de imposto de renda a ser pago em ativos de renda variável no mês atual. 
    /// </summary>
    [HttpGet("assets")]
    [AllowAnonymous]
    public IActionResult CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId) {
        var response = _service.CalculateAssetsIncomeTaxes(accountId);
        return Ok(response);
    }

    /// <summary>
    /// Calcula o imposto de renda de criptomoedas.
    /// </summary>
    [HttpPost("cryptocurrency")]
    public IActionResult CalculateCryptocurrency() {
        return Ok();
    }

    /// <summary>
    /// Calcula o imposto de renda de NFTs (lol).
    /// </summary>
    [HttpPost("nfts")]
    public IActionResult CalculateNFTs() {
        return Ok();
    }
}
