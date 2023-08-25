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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AppSettings() { }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
