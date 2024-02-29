using Api.DTOs.Auth;
using Infrastructure.Models;

namespace Api.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Registra um novo usuário no banco de dados e envia um e-mail de verificação.
        /// </summary>
        /// <param name="request">Objeto contendo todas as informações para o registro do usuário.</param>
        /// <returns>O id do usuário cadastrado.</returns>
        Task<Account> SignUp(SignUpRequest request);
    }
}
