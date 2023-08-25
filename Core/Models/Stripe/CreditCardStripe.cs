namespace Core.Models.Stripe
{
    public record CreditCardStripe(
        string Name,
        string CardNumber,
        string ExpirationYear,
        string ExpirationMonth,
        string Cvc
    );
}
