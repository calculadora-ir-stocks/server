namespace Common.Models.Secrets
{
    public class InfoSimplesSecret
    {
        public InfoSimplesSecret()
        {
            Secret = Environment.GetEnvironmentVariable("INFO_SIMPLES_TOKEN")!;
        }

        public string Secret { get; set; }
    }
}
