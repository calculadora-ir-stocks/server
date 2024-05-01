namespace Common.Options
{
    public class B3ApiOptions
    {
#pragma warning disable CS8618 // Configured as an Option<T> at DI.
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
#pragma warning restore CS8618
        public string GrantType = "client_credentials";
    }
}
