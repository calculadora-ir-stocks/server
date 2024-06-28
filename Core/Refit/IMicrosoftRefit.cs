using Api.DTOs.Auth;
using Refit;

namespace Core.Refit
{
    public interface IMicrosoftRefit
    {
        /// <summary>
        /// Obtém o token de autorização necessário para consumir as APIs da Área Logada no ambiente de produção.
        /// O token não deve ser gerado a cada requisição, mas sim em casos onde o token é expirado.
        /// </summary>
        /// <param name="data">o x-www-form-urlencoded contendo o ClientId e ClientSecret necessários</param>
        [Post("/aa5ac705-873b-4afc-a29d-f0adb89ccf5c/oauth2/v2.0/token")]
        Task<B3Token> GetAuthToken([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        /// <summary>
        /// Obtém o token de autorização necessário para consumir as APIs da Área Logada no ambiente de desenvolvimento.
        /// O token não deve ser gerado a cada requisição, mas sim em casos onde o token é expirado.
        /// </summary>
        /// <param name="data">o x-www-form-urlencoded contendo o ClientId e ClientSecret necessários</param>
        [Post("/4bee639f-5388-44c7-bbac-cb92a93911e6/oauth2/v2.0/token")]
        Task<B3Token> GetAuthTokenForDev([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
    }
}
