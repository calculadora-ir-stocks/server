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
    /// Obtém um usuário pelo Auth0 Id.
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
    /// <param name="cpf">CPF no formato <c>11111111111</c>.</param>
    [HttpGet("opt-in/{cpf}")]
    public async Task<IActionResult> OptIn(string cpf)
    {
        var didOptIn = await service.OptIn(cpf);
        return Ok(new { isAuthorized = didOptIn });
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await service.Delete(id);
        return Ok(new { message = $"A conta do usuário {id} foi deletada com sucesso e sua conta foi desvinculada com a B3." });
    }
}
