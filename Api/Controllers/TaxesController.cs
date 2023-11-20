using Core.Filters;
using Core.Models.Api.Responses;
using Core.Models.Responses;
using Core.Requests.BigBang;
using Core.Services.B3Syncing;
using Core.Services.DarfGenerator;
using Core.Services.Taxes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers;

/// <summary>
/// Responsável por calcular o imposto de renda dos ativos de renda variável.
/// </summary>
[Tags("Taxes")]
public class TaxesController : BaseController
{
    private readonly ITaxesService taxesService;
    private readonly IB3SyncingService syncingService;
    private readonly IDarfGeneratorService darfGeneratorService;

    public TaxesController(ITaxesService taxesService, IB3SyncingService syncingService, IDarfGeneratorService darfGeneratorService)
    {
        this.taxesService = taxesService;
        this.syncingService = syncingService;
        this.darfGeneratorService = darfGeneratorService;
    }

    /// <summary>
    /// Gera uma DARF para o usuário especificado referente a um mês onde há impostos a ser pago.
    /// </summary>
    /// <param name="accountId">O id do usuário.</param>
    /// <param name="month">O mês onde há impostos a ser pago que a DARF será gerada. Formato: MM/yyyy</param>
    /// <param name="value">Valor adicional (geralmente de meses onde houveram impostos inferiores a R$10,00) para ser
    /// somado no valor total da DARF.</param>
    /// <returns>O código de barras da DARF e outras informações referentes ao imposto sendo pago.</returns>
    [HttpGet("generate-darf")]
    [ProducesResponseType(typeof(DARFResponse), 200)]
    [ProducesResponseType(typeof(Core.Notification.Notification), 404)]
    public async Task<IActionResult> GenerateDarf(Guid accountId, string month, double value = 0)
    {
        var response = await darfGeneratorService.Generate(accountId, month, value);
        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos do mês atual.
    /// </summary>
    [HttpGet("home/{accountId}")]
    [ProducesResponseType(typeof(TaxesDetailsResponse), 200)]
    [ProducesResponseType(typeof(Core.Notification.Notification), 404)]
    public async Task<IActionResult> Home(Guid accountId)
    {
        var response = await taxesService.GetCurrentMonthTaxes(accountId);

        if (response.Movements.IsNullOrEmpty()) return NotFound("Por enquanto não há nenhum imposto de renda a ser pago.");

        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no mês especificado.
    /// </summary>
    /// <param name="month">Formato: MM/yyyy</param>
    /// <param name="accountId">O id do usuário</param>
    [HttpGet("details/{month}/{accountId}")]
    [ProducesResponseType(typeof(TaxesDetailsResponse), 200)]
    [ProducesResponseType(typeof(Core.Notification.Notification), 404)]
    public async Task<IActionResult> Details(string month, Guid accountId)
    {
        var response = await taxesService.Details(month, accountId);
        return Ok(response);
    }

    /// <summary>
    /// Retorna todas as informações referentes a impostos no ano especificado.
    /// </summary>
    /// <param name="year">Formato: yyyy</param>
    /// <param name="accountId">O id do usuário</param>
    [HttpGet("calendar/{year}/{accountId}")]
    [ProducesResponseType(typeof(CalendarResponse), 200)]
    [ProducesResponseType(typeof(Core.Notification.Notification), 404)]
    public async Task<IActionResult> Calendar(string year, Guid accountId)
    {
        var response = await taxesService.GetCalendarTaxes(year, accountId);
        return Ok(response);
    }

    /// <summary>
    /// Altera o mês especificado como pago/não pago.
    /// </summary>
    /// <param name="month">Formato: MM/yyyy</param>
    /// <param name="accountId">O id do usuário</param>
    [HttpPut("set-paid-or-unpaid/{month}/{accountId}")]
    [ProducesResponseType(typeof(Core.Notification.Notification), 200)]
    [ProducesResponseType(typeof(Core.Notification.Notification), 404)]
    public async Task<IActionResult> SetMonthAsPaid(string month, Guid accountId)
    {
        await taxesService.SetAsPaidOrUnpaid(month, accountId);
        return Ok(new { message = "O mês especificado foi alterado para pago/não pago com sucesso." });
    }

    /// <summary>
    /// Faz o cálculo de impostos retroativos e preço médio de todos os ativos da conta de um investidor.
    /// Deve ser executado uma única vez quando um usuário cadastrar-se na plataforma.
    /// </summary>
    [HttpPost("big-bang/{accountId}")]
    [ProducesResponseType(typeof(Core.Notification.Notification), 200)]
    [ProducesResponseType(typeof(Core.Notification.Notification), 404)]
    public async Task<IActionResult> BigBang(Guid accountId, [FromBody] List<BigBangRequest> request)
    { 
        await syncingService.Sync(accountId, request);
        return Ok(new { message = "Imposto de renda e preço médio mais recente calculados e armazenados com sucesso." });
    }
}
