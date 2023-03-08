namespace stocks_common
{
    public class AppSettings
    {
        public AppSettings(string secret, string issuer, string audience)
        {
            Secret = secret;
            Issuer = issuer;
            Audience = audience;
        }

        public AppSettings() { }

        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
