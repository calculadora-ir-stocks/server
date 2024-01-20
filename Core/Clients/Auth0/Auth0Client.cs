
using System.Net.Http.Headers;
using Common.Models.Secrets;
using Core.Models.Auth0;
using Newtonsoft.Json;
using Stripe;

namespace Core.Clients.Auth0
{
    public class Auth0Client : IAuth0Client
    {
        private readonly IHttpClientFactory factory;
        private readonly HttpClient client;

        private readonly Auth0Secret secret;

        public Auth0Client(IHttpClientFactory factory, Auth0Secret secret)
        {
            this.factory = factory;
            client = factory.CreateClient("Auth0");
            this.secret = secret;
        }

        public async Task<string> GetToken()
        {
            HttpRequestMessage request = new(HttpMethod.Post, "token")
            {
                Content = new StringContent(JsonConvert.SerializeObject(secret))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            string? json = await response.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<Auth0Token>(json);

            return token!.AccessToken;
        }
    }
}