using Api.DTOs.Auth;
using Api.Services.Auth;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    /// Registra um novo usuário na plataforma.
    /// </summary>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        var response = await service.SignUp(request);

        Response.Headers["Authorization"] = response.Jwt;

        return Ok(new { accountId = response.AccountId });
    }

    /// <summary>
    /// Insere no header <c>location</c> a URL do servidor de autenticação do Auth0.
    /// </summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn()
    {
        return Ok();
    }

    /// <summary>
    /// Insere no header <c>location</c> a URL do servidor de log-out do Auth0.
    /// </summary>
    [HttpPost("sign-out")]
    [AllowAnonymous]
    public async new Task<IActionResult> SignOut()
    {
        return Ok();
    }

    [HttpGet("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token()
    {
        return Ok(await service.GetToken());
    }

}
