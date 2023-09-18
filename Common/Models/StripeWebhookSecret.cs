namespace Common.Models
{
    public class StripeWebhookSecret
    {
        public StripeWebhookSecret(string secret)
        {
            Secret = secret;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public StripeWebhookSecret()
        {
        }

        public string Secret { get; set; }
    }
}
