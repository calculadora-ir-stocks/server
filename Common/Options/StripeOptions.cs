using System.ComponentModel.DataAnnotations;

namespace Common.Options
{
    public class StripeOptions
    {
#pragma warning disable CS8618 // Configured as an Option<T> at DI.
        [Required]
        public string WebhookToken { get; set; }
        [Required]
        public string ApiToken { get; set; }
#pragma warning restore CS8618
    }
}
