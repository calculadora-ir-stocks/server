namespace Common.Models.Secrets
{
    public class StripeSecret
    {
        public StripeSecret()
        {
            WebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_TOKEN")!;
            ApiSecret = Environment.GetEnvironmentVariable("STRIPE_API_TOKEN")!;
        }

        public string WebhookSecret { get; set; }
        public string ApiSecret { get; set; }
    }
}
