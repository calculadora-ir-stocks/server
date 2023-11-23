namespace Common
{
    public class JwtProperties
    {
        public JwtProperties(string token, string issuer, string audience)
        {
            Token = token;
            Issuer = issuer;
            Audience = audience;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public JwtProperties() { }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string Token { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Authoriry { get; set; }
    }
}
