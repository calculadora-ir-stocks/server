namespace stocks_core.Models.Stripe
{
    public record AddStripeCustomer(
        string Email,
        string Name,
        CreditCardStripe CreditCard
    );
}
