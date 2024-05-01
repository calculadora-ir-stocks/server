using Api.Clients.B3;
using Api.DTOs.Auth;
using Common.Options;
using Core.Models.B3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace Core.Clients.B3
{
    // TODO refit
    // TODO verificar se status code é 422. Se for, perdemos o vínculo do investidor com a API - notificar o front.
    public class B3Client : IB3Client
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly IOptions<B3ApiOptions> options;

        private readonly HttpClient b3Client;
        private readonly HttpClient microsoftClient;

        private static B3Token? token;

        private readonly ILogger<B3Client> logger;

        /// <summary>
        /// https://clientes.b3.com.br/data/files/50/E0/18/FD/B623F7107E3811F7BFC9F9C2/Informacoes%20de%20APIs%20%20Ambiente%20certificacao.pdf
        /// </summary>
        private const string B3TokenAuthorizationRequestUri = "/4bee639f-5388-44c7-bbac-cb92a93911e6/oauth2/v2.0/token";

        public B3Client(IHttpClientFactory clientFactory, IOptions<B3ApiOptions> options, ILogger<B3Client> logger)
        {
            this.clientFactory = clientFactory;
            this.options = options;

            b3Client = this.clientFactory.CreateClient("B3");
            microsoftClient = this.clientFactory.CreateClient("Microsoft");

            token = null!;
            this.logger = logger;
        }

#pragma warning disable CS8618
        public B3Client() { }
#pragma warning restore CS8618

        public async Task<Movement.Root?> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId, string? nextUrl)
        {
            Stopwatch watch = new();

            HttpRequestMessage request = new(
                HttpMethod.Get,
                $"movement/v2/equities/investors/{cpf}?referenceStartDate={referenceStartDate}&referenceEndDate={referenceEndDate}"
            );

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

                string? json = await response.Content.ReadAsStringAsync();
                var assets = JsonConvert.DeserializeObject<Movement.Root>(json);

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

        private async Task<B3Token> GetB3AuthorizationToken()
        {
            if (token is not null && !token!.Expired)
                return token;
            else
                return await RefreshAuthorizationToken();
        }

        private async Task<B3Token> RefreshAuthorizationToken()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, B3TokenAuthorizationRequestUri)
                {
                    Content = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
                    {
                        new("content-type", "application/x-www-form-urlencoded"),
                        new("client_id", options.Value.ClientId),
                        new("client_secret", options.Value.ClientSecret),
                        new("scope", "98ddf4b0-f66d4c96-97ea-9e30306599e7%2F.default"),
                        new("grant_type", options.Value.GrantType)
                    })
                };             

                using var response = await microsoftClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var responseContentStream = response.Content.ReadAsStringAsync().Result;

                token = JsonConvert.DeserializeObject<B3Token>(responseContentStream)!;

                return token ?? throw new Exception("Uma exceção ocorreu ao deserializar o objeto de Token de autenticação da B3");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Uma exceção ocorreu ao tentar obter o token de autorização da B3. {exception} ", e.Message);
                throw new Exception("Uma exceção ocorreu ao tentar obter o token de autorização da B3. " + e.Message);
            }
        }

        public async Task<bool> OptIn(string cpf)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"authorizations/investors/{cpf}");

            var accessToken = await GetB3AuthorizationToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await b3Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var authorized = JsonConvert.DeserializeObject<OptIn>(json)!.Data.Authorized;

            return authorized;
        }

        public async Task OptOut(string cpf)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"optout/investor/{cpf}");

            var accessToken = await GetB3AuthorizationToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await b3Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
