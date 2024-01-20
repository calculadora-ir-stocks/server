using Newtonsoft.Json;

namespace Api.DTOs.Auth
{
    public class B3Token
    {
        public B3Token(string accessToken, string scheme, int expiresInSeconds)
        {
            AccessToken = accessToken;
            Scheme = scheme;
            ExpiresInSeconds = expiresInSeconds;
            Expires = DateTime.Now.AddSeconds(expiresInSeconds);
        }


        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string Scheme { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; }

        public DateTime Expires { get; }

        public bool Expired => Expires <= DateTime.Now;
    }
}
