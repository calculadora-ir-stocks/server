using Api.DTOs.Auth;
using Core.Models.Api.Responses;

namespace Api.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Registra um novo usuário no banco de dados e envia um e-mail de verificação.
        /// </summary>
        /// <param name="request">Objeto contendo todas as informações para o registro do usuário.</param>
        /// <returns>O id do usuário cadastrado.</returns>
        Task<SignUpResponse> SignUp(SignUpRequest request);

        /// <summary>
        /// Autentica um usuário já cadastrado na plataforma
        /// </summary>
        /// <param name="request">Objeto contendo todas as informações para a autenticação do usuário.</param>
        /// <returns>Um token JWT se a autenticação for bem sucedida e o id do usuário autenticado.</returns>
        (string? Jwt, Guid Id) SignIn(SignInRequest request);

        /// <summary>
        /// Obtém o token de autenticação do Auth0.
        /// </summary>
        Task<string> GetToken();
    }
}
