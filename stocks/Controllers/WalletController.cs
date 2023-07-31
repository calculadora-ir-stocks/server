using Microsoft.AspNetCore.Mvc;
using stocks_core.Services.WalletService;

namespace stocks.Controllers;

[Tags("Wallet")]
public class WalletController : BaseController
{
    private readonly IWalletService service;

    public WalletController(IWalletService service)
    {
        this.service = service;
    }

    [HttpGet("assets/all/{id}")]
    public IActionResult GetAllAssets([FromRoute] Guid accountId)
    {
        var response = service.GetAllAssets(accountId);

        if (response is null) return NotFound("Nenhum ativo foi encontrado para o investidor especificado.");

        return Ok(response);
    }
}
