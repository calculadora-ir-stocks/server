using Api.DTOs.Auth;
using Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Responsável pelo registro e autenticação de usuários.
/// </summary>
[Tags("Authentication")]
[AllowAnonymous]
public class AuthController : BaseController
{
    private readonly IAuthService service;

    public AuthController(IAuthService service)
    {
        this.service = service;
    }

    /// <summary>
    /// Registra um novo usuário na plataforma.
    /// </summary>
    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        var response = await service.SignUp(request);

        Response.Headers["Authorization"] = response.Jwt;

        return Ok(new { accountId = response.AccountId });
    }

    /// <summary>
    /// Autentica um usuário na plataforma.
    /// </summary>
    [HttpPost("sign-in")]
    public IActionResult SignIn([FromBody] SignInRequest request)
    {
        var (Jwt, Id) = service.SignIn(request);

        if (Jwt is null)
            return BadRequest("Nome de usuário ou senha incorreto(s).");

        Response.Headers["Authorization"] = Jwt;

        return Ok(new { accountId = Id });
    }
}
