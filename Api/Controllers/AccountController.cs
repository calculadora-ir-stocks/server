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
    /// Deleta a conta especificada e desvincula com a B3.
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        service.Delete(id);
        return Ok(new { message = $"A conta do usuário {id} foi deletada com sucesso e sua conta foi desvinculada com a B3." });
    }

    /// <summary>
    /// Obtém o id do usuário através do id do Auth0.
    /// </summary>
    /// <param name="auth0Id">O id do Auth0.</param>
    [HttpGet("{auth0Id}")]
    public async Task<IActionResult> GetByAuth0Id([FromRoute] string auth0Id) 
    {
        Guid accountId = await service.GetByAuth0Id(auth0Id);
        return Ok(new { accountId });
    }
}
