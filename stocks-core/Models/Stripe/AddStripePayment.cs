namespace stocks_core.Models.Stripe
{
    public record AddStripePayment(
        string CustomerId,
        string ReceiptEmail,
        string Description,
        long Amount
    );
}
