namespace Core.Models.Stripe
{
    public record StripeCustomer(
        string Name,
        string Email,
        string CustomerId
    );
}
