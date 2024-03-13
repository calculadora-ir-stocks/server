using Api.DTOs.Auth;
using Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Responsável pelo registro e autenticação de usuários.
/// </summary>
[Tags("Authentication")]
public class AuthController : BaseController
{
    private readonly IAuthService service;

    public AuthController(IAuthService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Registra um usuário já criado anteriormente no Auth0.
    /// </summary>
    [HttpPost("sign-up")]
    [AllowAnonymous] // TODO remove for production
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        var account = await service.SignUp(request);
        if (account.IsInvalid) return BadRequest();
        return Ok(new { accountId = account.Id });
    }
}
