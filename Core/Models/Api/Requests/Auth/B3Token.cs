using System.Text.Json.Serialization;

namespace Api.DTOs.Auth
{
    public class B3Token
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string Scheme { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; set; }

        public DateTime Expires { get; private set; }
        public void SetExpiration() => Expires = DateTime.Now.AddSeconds(ExpiresInSeconds);
        public bool Expired => Expires <= DateTime.Now;
    }
}
