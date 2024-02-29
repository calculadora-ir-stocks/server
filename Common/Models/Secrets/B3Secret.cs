namespace Common.Models.Secrets
{
    public class B3Secret
    {
        public B3Secret()
        {
            ClientId = Environment.GetEnvironmentVariable("B3_CLIENT_ID")!;
            ClientSecret = Environment.GetEnvironmentVariable("B3_CLIENT_SECRET")!;
            Scope = Environment.GetEnvironmentVariable("B3_SCOPE")!;
            GrantType = Environment.GetEnvironmentVariable("B3_GRANT_TYPE")!;
        }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
        public string GrantType { get; set; }
    }
}
