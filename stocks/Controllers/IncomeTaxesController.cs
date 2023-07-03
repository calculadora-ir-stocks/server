using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using stocks.Services.IncomeTaxes;
using stocks_core.Requests.BigBang;
using stocks_core.Requests.IncomeTaxes;

namespace stocks.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Tags("Income taxes")]
public class IncomeTaxesController : BaseController
{
    private readonly IIncomeTaxesService service;

    public IncomeTaxesController(IIncomeTaxesService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Calcula o total de imposto de renda a ser pago em ativos de renda variável no mês atual. 
    /// O formato data deve ser yyyy-MM-dd.
    /// </summary>
    [HttpGet("assets")]
    public async Task<IActionResult> CalculateIncomeTaxesFromMonth([FromQuery] AssetsIncomeTaxesRequest request)
    {
        var response = await service.CalculateCurrentMonthAssetsIncomeTaxes(request);

        if (response.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        return Ok(response);
    }

    /// <summary>
    /// (Deve ser executado uma única vez quando um usuário cadastrar-se na plataforma)
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
    public IActionResult CalculateCryptocurrency() {
        return Ok();
    }

    /// <summary>
    /// Calcula o imposto de renda de NFTs (kkkkk).
    /// </summary>
    [HttpPost("nfts")]
    public IActionResult CalculateNFTs() {
        return Ok();
    }
}
