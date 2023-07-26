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

        private readonly HttpClient client;
        private readonly HttpClient tokenClient;

        private static Token? token;

        private static readonly SemaphoreSlim AccessTokenSemaphore = new(1, 1);
        private static readonly SemaphoreSlim GetAllMovementsSemaphore = new(MaximumConcurrentRequests);

        private long circuitStatus;
        private const long Closed = 0;
        private const long Tripped = 1;
        public string Unavailable = "Unavailable";

        // Número total de sockets a serem utilizados pelas requisições multi-thread de buscar todos os movimentos de um investidor.
        private const int MaximumConcurrentRequests = 4;

        private readonly ILogger<B3Client> logger;

        public B3Client(IHttpClientFactory clientFactory, ILogger<B3Client> logger)
        {
            this.clientFactory = clientFactory;

            client = this.clientFactory.CreateClient("B3");
            tokenClient = this.clientFactory.CreateClient("Microsoft");

            token = null!;

            circuitStatus = Closed;

            this.logger = logger;
        }

        public B3Client() { }

        public async Task<Movement.Root> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId, string? nextUrl)
        {
            Stopwatch watch = new();

            HttpRequestMessage request = new(HttpMethod.Get, $"movement/v2/equities/investors/{cpf}?referenceStartDate={referenceStartDate}&referenceEndDate={referenceEndDate}");

            // A API da B3 possui um sistema de paginação. O nextUrl é a URL da página seguinte do response.
            if (nextUrl != null)
            {
                request = new(HttpMethod.Get, nextUrl);
            }

            watch.Start();

            var accessToken = await GetB3AuthorizationToken();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var assets = JsonConvert.DeserializeObject<Movement.Root>(responseContentStream);

            // TODO: corrigir o multithreading
            await Task.Run(async () =>
            {
                await GetAccountMovementsInAllPages(assets);
            });

            watch.Stop();

            int? total = assets?.Data.EquitiesPeriods.EquitiesMovements.Count;
            long seconds = watch.ElapsedMilliseconds / 1000;

            logger.LogInformation("O usuário {accountId} executou o big bang e importou um total de {total} movimentações. O tempo" +
                "de execução foi de {seconds} segundos.", accountId, total, seconds);

            return assets;
        }

        /// <summary>
        /// Como a API da B3 separa o response por páginas, é necessário percorrê-las
        /// afim de obter todos os dados.
        /// </summary>
        private async Task GetAccountMovementsInAllPages(Movement.Root? root)
        {
            if (root is null) return;
            if (root.Links.Next is null) return;

            try
            {
                await GetAllMovementsSemaphore.WaitAsync();

                if (IsTripped()) return;

                HttpRequestMessage request = new(HttpMethod.Get, root.Links.Next);

                ServicePointManager.FindServicePoint(request.RequestUri).ConnectionLimit = MaximumConcurrentRequests;

                var accessToken = await GetB3AuthorizationToken();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    TripCircuit(reason: $"Status not OK. Status={response.StatusCode}");
                    return;
                }

                response.EnsureSuccessStatusCode();

                var responseContentStream = await response.Content.ReadAsStringAsync();

                var assets = JsonConvert.DeserializeObject<Movement.Root>(responseContentStream);

                root.Links.Next = assets.Links.Next;
                root.Data.EquitiesPeriods.EquitiesMovements.AddRange(assets.Data.EquitiesPeriods.EquitiesMovements);
            }
            catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                TripCircuit(reason: $"Timed out");
                return;
            }
            finally
            {
                GetAllMovementsSemaphore.Release();
            }

            // https://t.ly/p27y
            await GetAccountMovementsInAllPages(root);
        }

        public async Task<Position.Root> GetAccountPosition(string cpf, string referenceDate, string? nextUrl = null)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"position/v2/equities/investors/{cpf}?referenceDate={referenceDate}");

            if (nextUrl != null)
            {
                request = new(HttpMethod.Get, nextUrl);
            }

            var accessToken = await GetB3AuthorizationToken();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var assets = JsonConvert.DeserializeObject<Position.Root>(responseContentStream);

            return assets!;
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
                await AccessTokenSemaphore.WaitAsync();

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

                using var response = await tokenClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var responseContentStream = response.Content.ReadAsStringAsync().Result;

                token = JsonConvert.DeserializeObject<Token>(responseContentStream)!;

                return token ?? throw new Exception("Uma exceção ocorreu ao deserializar o objeto de Token de autenticação da B3");
            }
            catch (Exception e)
            {
                logger.LogError("Uma exceção ocorreu ao tentar obter o token de autorização da B3. {exception} ", e.Message);
                throw new Exception("Uma exceção ocorreu ao tentar obter o token de autorização da B3. " + e.Message);
            }
            finally
            {
                AccessTokenSemaphore.Release(1);
            }
        }

        private void TripCircuit(string reason)
        {
            if (Interlocked.CompareExchange(ref circuitStatus, Tripped, Closed) == Closed)
            {
                Console.WriteLine($"Tripping circuit because: {reason}");
            }
        }

        private bool IsTripped()
        {
            return Interlocked.Read(ref circuitStatus) == Tripped;
        }
    }
}
