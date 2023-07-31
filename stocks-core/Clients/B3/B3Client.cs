using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using stocks.Clients.B3;
using stocks.DTOs.Auth;
using stocks_core.DTOs.B3;

namespace stocks.Services.B3
{
    public class B3Client : IB3Client
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly HttpClient b3Client;
        private readonly HttpClient microsoftClient;

        private static Token? token;

        private readonly ILogger<B3Client> logger;

        public B3Client(IHttpClientFactory clientFactory, ILogger<B3Client> logger)
        {
            this.clientFactory = clientFactory;

            b3Client = this.clientFactory.CreateClient("B3");
            microsoftClient = this.clientFactory.CreateClient("Microsoft");

            token = null!;

            this.logger = logger;
        }

        public B3Client() { }

        public async Task<Movement.Root?> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId, string? nextUrl)
        {
            Stopwatch watch = new();

            HttpRequestMessage request = new(HttpMethod.Get, $"movement/v2/equities/investors/{cpf}?referenceStartDate={referenceStartDate}&referenceEndDate={referenceEndDate}");

            if (nextUrl != null)
            {
                request.RequestUri = new Uri(nextUrl);
            }

            watch.Start();

            var accessToken = await GetB3AuthorizationToken();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await b3Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var assets = JsonConvert.DeserializeObject<Movement.Root>(responseContentStream);

            await GetAccountMovementsInAllPages(assets);

            watch.Stop();

            int? total = assets?.Data.EquitiesPeriods.EquitiesMovements.Count;
            long seconds = watch.ElapsedMilliseconds / 1000;

            logger.LogInformation("O usuário {accountId} importou um total de {total} movimentações. O tempo" +
                "de execução foi de {seconds} segundos.", accountId, total, seconds);

            return assets;
        }

        /// <summary>
        /// Como a API da B3 separa o response por páginas, é necessário percorrê-las
        /// afim de obter todos os dados.
        /// </summary>
        private async Task GetAccountMovementsInAllPages(Movement.Root? root)
        {
            if (root is null || root.Links.Next is null) return;

            try
            {
                HttpRequestMessage request = new(HttpMethod.Get, root.Links.Next);

                ServicePointManager.FindServicePoint(request.RequestUri).ConnectionLimit = 5;

                var accessToken = await GetB3AuthorizationToken();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

                using var response = await b3Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                string? responseContentStream = await response.Content.ReadAsStringAsync();

                var assets = JsonConvert.DeserializeObject<Movement.Root>(responseContentStream);

                root.Links.Next = assets.Links.Next;
                root.Data.EquitiesPeriods.EquitiesMovements.AddRange(assets.Data.EquitiesPeriods.EquitiesMovements);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao executar o método {method}, {message}",
                    nameof(GetAccountMovementsInAllPages),
                    e.Message
                );
                return;
            }

            // https://t.ly/p27y
            await GetAccountMovementsInAllPages(root);
        }

        private async Task<Token> GetB3AuthorizationToken()
        {
            if (token is not null && !token!.Expired)
                return token;
            else
                return await RefreshAuthorizationToken();
        }

        private async Task<Token> RefreshAuthorizationToken()
        {
            try
            {
                if (token is not null && !token.Expired) return token;

                var request = new HttpRequestMessage(HttpMethod.Post, "4bee639f-5388-44c7-bbac-cb92a93911e6/oauth2/v2.0/token/")
                {
                    Content = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
                    {
                        new("client_id", "6d005c50-8c54-4874-9049-18d3fbb6f1e0"),
                        new("client_secret", "lD98Q~WZtjqE1gvILoOKNgO~7pTMO2SkOlJeIdo2"),
                        new("scope", "0c991613-4c90-454d-8685-d466a47669cb/.default"),
                        new("grant_type", "client_credentials")
                    })
                };

                using var response = await microsoftClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var responseContentStream = response.Content.ReadAsStringAsync().Result;

                token = JsonConvert.DeserializeObject<Token>(responseContentStream)!;

                return token ?? throw new Exception("Uma exceção ocorreu ao deserializar o objeto de Token de autenticação da B3");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Uma exceção ocorreu ao tentar obter o token de autorização da B3. {exception} ", e.Message);
                throw new Exception("Uma exceção ocorreu ao tentar obter o token de autorização da B3. " + e.Message);
            }
        }
    }
}
