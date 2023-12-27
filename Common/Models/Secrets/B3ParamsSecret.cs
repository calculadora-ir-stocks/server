namespace Common.Models.Secrets
{
    public class B3ParamsSecret
    {
        public B3ParamsSecret(string clientId, string clientSecret, string scope, string grantType)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            Scope = scope;
            GrantType = grantType;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public B3ParamsSecret()
        {
        }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
        public string GrantType { get; set; }
    }
}
