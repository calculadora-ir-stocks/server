using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.DTOs.Auth;
using Api.Services.Auth;

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
    /// Registra um novo usuário na plataforma.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sign-up")]
    public IActionResult SignUp([FromBody] SignUpRequest request)
    {
        service.SignUp(request);
        return Ok(200);
    }

    /// <summary>
    /// Autentica um usuário na plataforma.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sign-in")]
    public IActionResult SignIn([FromBody] SignInRequest request)
    {
        string? jwt = service.SignIn(request);

        if (jwt is null)
            return BadRequest("Nome de usuário ou senha incorreto(s).");

        return Ok(jwt);
    }
}
