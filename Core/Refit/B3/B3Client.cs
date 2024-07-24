using Api.DTOs.Auth;
using Azure;
using common.Helpers;
using Common.Options;
using Core.Models.B3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using System.Diagnostics;
using System.Net;
using static System.Net.WebRequestMethods;

namespace Core.Refit.B3
{
    public class B3Client : IB3Client
    {
        private readonly IB3Refit b3Client;
        private readonly IMicrosoftRefit microsoftClient;

        private readonly IOptions<B3ApiOptions> options;

        private static B3Token? token = null;
        private readonly ILogger<B3Client> logger;

        public B3Client(IB3Refit b3Client, IMicrosoftRefit microsoftClient, IOptions<B3ApiOptions> options, ILogger<B3Client> logger)
        {
            this.b3Client = b3Client;
            this.microsoftClient = microsoftClient;
            this.options = options;
            this.logger = logger;
        }

        public async Task<HttpStatusCode> B3HealthCheck()
        {
            string b3Token = await GetOrGenerateAuthToken();
            var response = await b3Client.B3HealthCheck(b3Token);

            return response.StatusCode;
        }        

        public async Task<Movement.Root?> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId)
        {
            Stopwatch watch = new();
            watch.Start();

            var accessToken = await GetOrGenerateAuthToken();

            var response = await b3Client.GetAccountMovements(accessToken, UtilsHelper.RemoveSpecialCharacters(cpf), referenceStartDate, referenceEndDate);

            if (response.Content is null) return null;

            if (response.Content.Links.Next is not null)
                await GetAccountMovementsInAllPages(response.Content);

            watch.Stop();

            int? total = response?.Content.Data.EquitiesPeriods.EquitiesMovements.Count;
            long seconds = watch.ElapsedMilliseconds / 1000;

            logger.LogInformation("O usuário {accountId} importou um total de {total} movimentações. O tempo " +
                "de execução foi de {seconds} segundos.", accountId, total, seconds);

            // A B3 retorna as últimas operações como as primeiras da lista. Todos os serviços que consomem esse endpoint
            // querem a ordem contrária.
            response.Content.Data.EquitiesPeriods.EquitiesMovements.Reverse();

            return response.Content;
        }

        /// <summary>
        /// Como a API da B3 separa o response por páginas, é necessário percorrê-las
        /// afim de obter todos os dados.
        /// </summary>
        private async Task GetAccountMovementsInAllPages(Movement.Root root)
        {
            try
            {
                var accessToken = await GetOrGenerateAuthToken();

                // TODO não urgente: abstrair baseUrlSize pra funcionar no ambiente de dev

                // A B3 retorna a URL completa como a próxima página. Não podemos usar ela porque o Refit
                // não vai permitir passar uma URL com baseUrl no parâmetro. Por isso, a baseURL é removida.
                int baseUrlSize = "https://investidor.b3.com.br:2443/api/".Length;
                var url = root.Links.Next!.Remove(0, baseUrlSize);

                var assets = await b3Client.GetAccountMovementsByPage(accessToken, url);
                root!.Links.Next = assets?.Links.Next;
                root.Data.EquitiesPeriods.EquitiesMovements.AddRange(assets!.Data.EquitiesPeriods.EquitiesMovements);

                if (root.Links.Next is null) return;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }

            await GetAccountMovementsInAllPages(root);
        }

        public async Task<bool> OptIn(string cpf)
        {
            var accessToken = await GetOrGenerateAuthToken();

            var response = await b3Client.OptIn(accessToken, UtilsHelper.RemoveSpecialCharacters(cpf));
            return response.Data.Authorized;
        }

        public async Task<ApiResponse<object>> OptOut(string cpf)
        {
            var accessToken = await GetOrGenerateAuthToken();
            var response = await b3Client.OptOut(accessToken, UtilsHelper.RemoveSpecialCharacters(cpf));

            return response;
        }

        private async Task<string> GetOrGenerateAuthToken()
        {
            if (token is null || token.Expired)
                return await GenerateAuthToken();
            else
                return token.AccessToken;
        }

        private async Task<string> GenerateAuthToken()
        {
            var xWwwFormUrlEncoded = new Dictionary<string, object> {
                { "grant_type", "client_credentials" },
                { "client_id", options.Value.ClientId },
                { "client_secret", options.Value.ClientSecret },
                { "scope", options.Value.Scope }
            };

            token = await microsoftClient.GetAuthToken(xWwwFormUrlEncoded);
            token.SetExpiration();

            logger.LogInformation($"Token da autenticação da Microsoft obtido às {DateTime.Now}");

            return token.AccessToken;
        }
    }
}
