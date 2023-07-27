using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using stocks.Services.IncomeTaxes;
using stocks_core.Requests.BigBang;
using stocks_core.Services.Hangfire;

namespace stocks.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Tags("Income taxes")]
public class IncomeTaxesController : BaseController
{
    private readonly IAssetsService service;

    public IncomeTaxesController(IAssetsService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Calcula o total de imposto de renda a ser pago em ativos de renda variável no mês atual.
    /// </summary>
    [HttpGet("assets/current/{accountId}")]
    public async Task<IActionResult> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId)
    {
        var response = await service.CalculateCurrentMonthAssetsIncomeTaxes(accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        // TO-DO: alterar AssetId pelo nome do ativo.
        return Ok(response);
    }

    /// <summary>
    /// Retorna o total de imposto de renda a ser pago em ativos de renda variável no mês especificado.
    /// </summary>
    [HttpGet("assets/{month}/{accountId}")]
    public async Task<IActionResult> CalculateSpecifiedMonthAssetsIncomeTaxes(string month, Guid accountId)
    {
        var response = await service.CalculateSpecifiedMonthAssetsIncomeTaxes(month, accountId);

        if (response.TradedAssets.IsNullOrEmpty()) return NotFound("Nenhum imposto de renda foi encontrado para o mês especificado.");

        // TO-DO: alterar AssetId pelo nome do ativo.
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
