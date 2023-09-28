using Core.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// [Authorize]
[Tags("Account")]
public class AccountController : BaseController
{
    private readonly IAccountService service;

    public AccountController(IAccountService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Envia um e-mail de verificação para um usuário. Para confirmar esse código,
    /// acesse /api/account/validate-code/
    /// </summary>
    /// <param name="accountId">O id da conta</param>
    [HttpPost("send-code/{accountId}")]
    public async Task<IActionResult> SendEmailVerification([FromRoute] Guid accountId)
    {
        await service.SendEmailVerification(accountId);
        return Ok(new { message = $"E-mail enviado para o usu�rio de id {accountId}" });
    }

    /// <summary>
    /// Retorna verdadeiro caso o código de validação seja válido, falso caso contrário.
    /// </summary>
    /// <param name="code">O código que o usuário quer confirmar</param>
    /// <param name="accountId">O id da conta</param>
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
    /// <param name="accountId">O id da conta</param>
    /// <returns></returns>
    [HttpGet("is-synced/{accountId}")]
    public IActionResult IsSynced(Guid accountId)
    {
        var response = service.IsSynced(accountId);
        return Ok(new { isSynced = response } );
    }

    /// <summary>
    /// Atualiza a senha da conta cadastrada.
    /// </summary>
    [HttpPut("password")]
    public IActionResult UpdatePassword(Guid accountId, string password)
    {
        service.UpdatePassword(accountId, password);
        return Ok(new { message = $"A senha do usu�rio {accountId} foi alterada com sucesso." });
    }

    /// <summary>
    /// Envia um e-mail de confirmação para o usuário com base no e-mail inserido.
    /// Para confirmar esse código, acesse /api/account/validate-code/
    /// </summary>
    /// <param name="email">O e-mail da conta.</param>
    /// <returns></returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] string email)
    {
        Guid accountId = await service.ForgotPassword(email);
        return Ok(new { message = $"E-mail enviado para o usu�rio de id {accountId}", accountId = accountId });
    }

    /// <summary>
    /// Deleta a conta especificada assim como desvincula com a B3.
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        service.Delete(id);
        return Ok(new { message = $"A conta do usuário {id} foi deletada com sucesso e sua conta foi desvinculada com a B3." });
    }
}
