using Microsoft.AspNetCore.Mvc;
using stocks_core.Services.AverageTradedPrice;

namespace stocks.Controllers
{
    /// <summary>
    /// Responsável por realizar o cálculo e a inserção no banco de dados do preço médio de cada ativo de um determinado investidor.
    /// </summary>
    [Tags("Average Traded Price")]
    public class AverageTradedPriceController : BaseController
    {
        private readonly IAverageTradedPriceService _service;

        public AverageTradedPriceController(IAverageTradedPriceService service)
        {
            _service = service;
        }

        /// <summary>
        /// Caclula e armazena no banco de dados, caso não exista, o preço médio de cada ativo do investidor levando em consideração todas as compras
        /// feitas do dia 01/11/2019 até D-1.
        /// </summary>
        [HttpPost("insert")]
        public async Task<IActionResult> InsertAverageTradedPrice([FromBody] Guid accountId)
        {
            await _service.Insert(accountId);
            return Ok("Preço médio calculado e armazenado com sucesso.");
        }
    }
}
