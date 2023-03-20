using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stocks.Requests;
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
    /// Calcula e armazena o imposto de renda a ser pago em todos os meses desde 01/11/2019 até D-1.
    /// Também calcula e armazena o preço médio de todos os ativos.
    /// Deve ser executado uma única vez quando um usuário cadastrar-se na plataforma.
    /// </summary>
    [HttpPost("big-bang")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculateIncomeTaxesForEveryMonth([FromRoute] Guid accountId,
        [FromBody] List<CalculateIncomeTaxesForEveryMonthRequest> request)
    {
        await _service.CalculateIncomeTaxesForEveryMonth(accountId, request);
        return Ok("Preço médio calculado e armazenado com sucesso.");
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
