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
        Task<Guid?> SignUp(SignUpRequest request);

        /// <summary>
        /// Obtém o token de autenticação do Auth0. Será usado apenas para testes locais. Em produção, o token JWT
        /// será requisitado para o Auth0 através do front-end.
        /// </summary>
        Task<string> GetToken();
    }
}
