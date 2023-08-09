using Microsoft.AspNetCore.Authorization;
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
    /// Envia um código de verificação por e-mail.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("send-code/{id}")]
    public async Task<IActionResult> SendEmailVerification([FromRoute] Guid id)
    {
        await service.SendEmailVerification(id);
        return Ok($"E-mail enviado para o usuário de id {id}");
    }

    /// <summary>
    /// Retorna verdadeiro caso o código de validação seja válido, falso caso contrário.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate-code/{accountId}")]
    public IActionResult IsEmailVerificationCodeValid([FromBody] string code, [FromRoute] Guid accountId)
    {
        bool isValid = service.IsEmailVerificationCodeValid(accountId, code);
        return Ok(isValid);
    }

    /// <summary>
    /// Atualiza a senha da conta cadastrada.
    /// </summary>
    [HttpPut("/password")]
    public IActionResult UpdatePassword(Guid accountId, string password)
    {
        service.UpdatePassword(accountId, password);
        return Ok($"A senha do usuário {accountId} foi alterada com sucesso.");
    }

    /// <summary>
    /// Deleta a conta especificada assim como desvincula com a B3.
    /// </summary>
    [HttpDelete("/{id}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        service.Delete(id);
        return Ok($"A conta do usuário {id} foi deletada com sucesso e sua conta foi desvinculada com a B3.");
    }
}
