using Api.DTOs.Auth;

namespace Api.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Registra um novo usuário no banco de dados e envia um e-mail de verificação.
        /// </summary>
        /// <param name="request">Objeto contendo todas as informações para o registro do usuário.</param>
        /// <returns>O id do usuário cadastrado.</returns>
        Task<Guid> SignUp(SignUpRequest request);

        /// <summary>
        /// Autentica um usuário já cadastrado na plataforma
        /// </summary>
        /// <param name="request">Objeto contendo todas as informações para a autenticação do usuário.</param>
        /// <returns>Um token JWT se a autenticação for bem sucedida.</returns>
        string? SignIn(SignInRequest request);
    }
}
