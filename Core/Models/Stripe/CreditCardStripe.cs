namespace stocks_core.Models.Stripe
{
    public record CreditCardStripe(
        string Name,
        string CardNumber,
        string ExpirationYear,
        string ExpirationMonth,
        string Cvc
    );
}
