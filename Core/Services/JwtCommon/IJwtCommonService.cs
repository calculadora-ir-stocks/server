using Common.Models;

namespace Api.Services.JwtCommon
{
    public interface IJwtCommonService
    {

        /// <summary>
        /// Gera um token JWT.
        /// </summary>
        public string GenerateToken(JwtContent account);

        /// <summary>
        /// Valida o token JWT especificado.
        /// </summary>
        /// <param name="token">O token JWT a ser validado.</param>
        /// <returns>O id do usuário se o token for válido, <c>null</c> se inválido.</returns>
        public Guid? ValidateJWTToken(string token);
    }
}
