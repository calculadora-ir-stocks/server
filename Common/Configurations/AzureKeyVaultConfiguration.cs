using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Common.Configurations
{
    public class AzureKeyVaultConfiguration
    {
        private const string keyVaultUri = "https://server-keys-and-secrets.vault.azure.net/";

        public AzureKeyVaultConfiguration()
        {
            SecretOptions = new()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                }
            };
            SecretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential(), SecretOptions);
        }

        public SecretClientOptions SecretOptions { get; init; }
        public SecretClient SecretClient { get; set; }
        // TODO configure keys
    }
}
