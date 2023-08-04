using Microsoft.AspNetCore.Mvc;
using stocks_core.Services.Account;

namespace stocks.Controllers;

// TODO [Authorize]
public class AccountController : BaseController
{
    private readonly IAccountService service;

    public AccountController(IAccountService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Atualiza a senha da conta cadastrada.
    /// </summary>
    [HttpPut("/password/update")]
    public IActionResult UpdatePassword(Guid accountId, string password)
    {
        service.UpdatePassword(accountId, password);
        return Ok($"A senha do usuário {accountId} foi alterada com sucesso.");
    }
}
