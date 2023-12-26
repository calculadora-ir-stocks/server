
using System.Net.Http.Headers;
using Stripe;

namespace Core.Clients.Auth0
{
    public class Auth0Client : IAuth0Client
    {
        private readonly IHttpClientFactory factory;
        private readonly HttpClient client;

        public Auth0Client(IHttpClientFactory factory)
        {
            this.factory = factory;
            client = factory.CreateClient("dsd");
        }

        public async Task<string> GetToken()
        {
            HttpRequestMessage request = new(HttpMethod.Post, "token")
            {
                Content = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
                {
                    // TODO get from appsettings
                    new("application/json", "{\"client_id\":\"ILzN6bW5L0atuNgVyINGYyNYjW5cj8Ub\",\"client_secret\":\"8b2PUfCDwZ5ufQqNuIx3T4YtI80Sag568_mdMACiG2-tyoR5LKOBouzzJPqdkm7c\",\"audience\":\"https://stocks.com/\",\"grant_type\":\"client_credentials\"}"),
                })
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}