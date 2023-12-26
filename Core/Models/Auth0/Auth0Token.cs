namespace Core.Models.Auth0
{
    public record Auth0Token
    {
        public string access_token { get; init; }
    }
}