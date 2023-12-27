using Newtonsoft.Json;

namespace Core.Models.Auth0
{
    public record Auth0Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; init; }
    }
}