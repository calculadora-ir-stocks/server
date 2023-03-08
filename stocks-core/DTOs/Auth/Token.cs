using Newtonsoft.Json;

namespace stocks.DTOs.Auth
{
    public class Token
    {
        public Token(string accessToken, string scheme, int expiresInSeconds)
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
