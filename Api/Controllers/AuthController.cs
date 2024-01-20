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
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        Guid? accountId = await service.SignUp(request);
        return Ok(new { accountId });
    }

    /// <summary>
    /// Obtém o token de autenticação do Auth0. Será usado apenas para testes locais. Em produção, o token JWT
    /// será requisitado para o Auth0 através do front-end.
    /// </summary>
    [HttpGet("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token()
    {
        string token = await service.GetToken();
        return Ok(new { token });
    }
}
