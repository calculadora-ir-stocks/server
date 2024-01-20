namespace Common.Models.Secrets
{
    public class StripeSecret
    {
        public StripeSecret(string webhookSecret, string apiSecret)
        {
            WebhookSecret = webhookSecret;
            ApiSecret = apiSecret;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public StripeSecret()
        {
        }

        public string WebhookSecret { get; set; }
        public string ApiSecret { get; set; }
    }
}
