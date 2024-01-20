namespace Common.Models.Secrets
{
    public class InfoSimplesSecret
    {
        public InfoSimplesSecret(string secret)
        {
            Secret = secret;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public InfoSimplesSecret()
        {
        }

        public string Secret { get; set; }
    }
}
