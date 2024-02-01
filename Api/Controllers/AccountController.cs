using Core.Services.Account;
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
    /// Obtém um usuário pelo Auth0 Id.
    /// </summary>
    [HttpGet("{auth0Id}")]
    public async Task<IActionResult> GetByAuth0Id([FromRoute] string auth0Id)
    {
        Guid accountId = await service.GetByAuth0Id(auth0Id);
        if (accountId == Guid.Empty) return NotFound();
        return Ok(new { accountId });
    }

    /// <summary>
    /// Deleta a conta especificada e desvincula com a B3.
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        service.Delete(id);
        return Ok(new { message = $"A conta do usuário {id} foi deletada com sucesso e sua conta foi desvinculada com a B3." });
    }
}
