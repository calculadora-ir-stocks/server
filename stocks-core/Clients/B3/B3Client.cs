using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using stocks.Clients.B3;
using stocks.DTOs.Auth;
using stocks_core.DTOs.B3;
using System.Net;
using System.Net.Http.Headers;

namespace stocks.Services.B3
{
    public class B3Client : IB3Client
    {
        private readonly IHttpClientFactory _clientFactory;

        private readonly HttpClient _client;
        private readonly HttpClient _tokenClient;

        private static Token? _token;

        private static SemaphoreSlim AccessTokenSemaphore;
        private static SemaphoreSlim GetAllMovementsSemaphore;

        private long circuitStatus;
        private const long Closed = 0;
        private const long Tripped = 1;
        public string Unavailable = "Unavailable";

        // Número total de sockets a serem utilizados pelas requisições multi-thread de buscar todos os movimentos de um investidor.
        private const int MaximumConcurrentRequests = 4;

        private ILogger<B3Client> _logger;

        public B3Client(IHttpClientFactory clientFactory, ILogger<B3Client> logger)
        {
            _clientFactory = clientFactory;

            _client = _clientFactory.CreateClient("B3");
            _tokenClient = _clientFactory.CreateClient("Microsoft");

            _token = null!;

            AccessTokenSemaphore = new SemaphoreSlim(1, 1);            
            GetAllMovementsSemaphore = new SemaphoreSlim(MaximumConcurrentRequests);

            circuitStatus = Closed;

            _logger = logger;
        }

        public B3Client() { }

        public async Task<Movement.Root> GetAccountMovement(string cpf, string? referenceStartDate, string? referenceEndDate, string? nextUrl)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"movement/v2/equities/investors/{cpf}?referenceStartDate={referenceStartDate}&referenceEndDate={referenceEndDate}");

            // O nextUrl é um parâmetro retornado pelos endpoints da B3 que representa a próxima página do response.
            // Quando um endpoint é consumido, os dados são separados entre várias páginas para evitar um processamento de dados pesado.
            if (nextUrl != null)
            {
                request = new(HttpMethod.Get, nextUrl);
            }

            var accessToken = await GetB3AuthorizationToken();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var assets = JsonConvert.DeserializeObject<Movement.Root>(responseContentStream);

            await Task.Run(async () =>
            {
                await GetAccountMovementsInAllPages(assets);
            });

            return assets!;
        }

        /// <summary>
        /// Como a API da B3 separa o response por páginas, é necessário percorrê-las
        /// afim de obter todos os dados.
        /// </summary>
        private async Task GetAccountMovementsInAllPages(Movement.Root root)
        {
            if (root.Links.Next == null) return;

            try
            {
                await GetAllMovementsSemaphore.WaitAsync();

                if (IsTripped()) return;

                HttpRequestMessage request = new(HttpMethod.Get, root.Links.Next);

                ServicePointManager.FindServicePoint(request.RequestUri).ConnectionLimit = MaximumConcurrentRequests;

                var accessToken = await GetB3AuthorizationToken();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

                using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

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
            } catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                TripCircuit(reason: $"Timed out");
                return;
            } finally
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

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var assets = JsonConvert.DeserializeObject<Position.Root>(responseContentStream);

            return assets!;
        }

        private async Task<Token> GetB3AuthorizationToken()
        {
            if (_token is not null && !_token!.Expired)
                return _token;
            else
                return await RefreshAuthorizationToken();
        }

        private async Task<Token> RefreshAuthorizationToken()
        {
            try
            {
                await AccessTokenSemaphore.WaitAsync();

                if (_token is not null && !_token.Expired) return _token;

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

                using var response = await _tokenClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var responseContentStream = response.Content.ReadAsStringAsync().Result;

                _token = JsonConvert.DeserializeObject<Token>(responseContentStream)!;

                return _token ?? throw new Exception("Uma exceção ocorreu ao deserializar o objeto de Token de autenticação da B3");
            }
            catch (Exception e)
            {
                _logger.LogError("Uma exceção ocorreu ao tentar obter o token de autorização da B3. {exception} ", e.Message);
                throw new Exception("Uma exceção ocorreu ao tentar obter o token de autorização da B3. " + e.Message);
            }
            finally
            {
                AccessTokenSemaphore.Release(1);
            }
        }

        public void CloseCircuit()
        {
            if (Interlocked.CompareExchange(ref circuitStatus, Closed, Tripped) == Tripped)
            {
                Console.WriteLine("Closed circuit");
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
