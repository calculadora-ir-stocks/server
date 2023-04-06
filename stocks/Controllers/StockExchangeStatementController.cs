using Microsoft.AspNetCore.Mvc;

namespace stocks.Controllers;

/// <summary>
/// Responsável por definir quais ativos devem ser declarados no IRPF.
/// A declaração é anual e é sempre referente ao ano anterior.
/// </summary>
[Tags("Stock exchange statement")]
public class StockExchangeStatementController : BaseController
{

    /// <summary>
    /// Pesquisa por todos os ativos que precisam ser declarados na aplicação da Receita Federal.
    /// </summary>
    [HttpGet("search")]
    public IActionResult SearchForPendingStatements() {
        return Ok();
    }
}
