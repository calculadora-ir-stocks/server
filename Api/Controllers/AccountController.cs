using Core.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Tags("Account")]
public class AccountController : BaseController
{
    private readonly IAccountService service;

    public AccountController(IAccountService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Obtém um id de usuário pelo Auth0 Id.
    /// </summary>
    /// Esse endpoint atualmente é público pois o front-end precisa do id de usuário e não consegue gerar o JWT
    /// antes de inserir todos os claims necessários.
    [AllowAnonymous]
    [HttpGet("{auth0Id}")]
    public async Task<IActionResult> GetByAuth0Id([FromRoute] string auth0Id)
    {
        Guid accountId = await service.GetByAuth0Id(auth0Id);
        if (accountId == Guid.Empty) return NotFound();
        return Ok(new { accountId });
    }

    /// <summary>
    /// Verifica se o usuário realizou o opt-in com a B3.
    /// Deve ser consumido após o usuário abrir o link de opt-in com a B3.
    /// </summary>
    [HttpGet("opt-in/{accountId}")]
    public async Task<IActionResult> OptIn(Guid accountId)
    {
        // TODO uncomment for production
        // var didOptIn = await service.OptIn(accountId);
        return Ok(new { isAuthorized = true });
    }

    /// <summary>
    /// Acessa o link de opt-in da B3.
    /// </summary>
    [HttpGet("opt-in/link")]
    public IActionResult OptInLink()
    {
        string optInLink = service.GetOptInLink();
        return Ok(new { link = optInLink });
    }

    /// <summary>
    /// Deleta a conta especificada e desvincula com a B3.
    /// </summary>
    [HttpDelete("{accountId}")]
    public async Task<IActionResult> Delete([FromRoute] Guid accountId)
    {
        await service.Delete(accountId);
        return Ok(new { message = $"A conta do usuário {accountId} foi deletada com sucesso e sua conta foi desvinculada com a B3." });
    }
}
