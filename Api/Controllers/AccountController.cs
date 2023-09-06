using Core.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[Tags("Account")]
public class AccountController : BaseController
{
    private readonly IAccountService service;

    public AccountController(IAccountService service)
    {
        this.service = service;
    }

    [HttpPost("send-code/{accountId}")]
    public async Task<IActionResult> SendEmailVerification([FromRoute] Guid accountId)
    {
        await service.SendEmailVerification(accountId);
        return Ok(new { message = $"E-mail enviado para o usu�rio de id {accountId}" });
    }

    /// <summary>
    /// Retorna verdadeiro caso o código de validação seja válido, falso caso contrário.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate-code/{accountId}")]
    public IActionResult IsEmailVerificationCodeValid([FromBody] string code, [FromRoute] Guid accountId)
    {
        bool response = service.IsEmailVerificationCodeValid(accountId, code);
        return Ok(new { IsValid = response });
    }

    /// <summary>
    /// Determina se uma conta foi ou não sincronizada com a B3.
    /// </summary>
    /// <param name="accountId"></param>
    /// <returns></returns>
    [HttpGet("/is-synced/{accountId}")]
    public IActionResult IsSynced(Guid accountId)
    {
        var response = service.IsSynced(accountId);
        return Ok(new { isSynced = response } );
    }

    /// <summary>
    /// Atualiza a senha da conta cadastrada.
    /// </summary>
    [HttpPut("/password")]
    public IActionResult UpdatePassword(Guid accountId, string password)
    {
        service.UpdatePassword(accountId, password);
        return Ok(new { message = $"A senha do usu�rio {accountId} foi alterada com sucesso." });
    }

    /// <summary>
    /// Deleta a conta especificada assim como desvincula com a B3.
    /// </summary>
    [HttpDelete("/{id}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        service.Delete(id);
        return Ok(new { message = $"A conta do usuário {id} foi deletada com sucesso e sua conta foi desvinculada com a B3." });
    }
}
