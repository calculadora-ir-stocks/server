namespace Infrastructure.Models
{
    /// <summary>
    /// Quando um usuário faz uma autenticação com o Stripe para assinar um plano, por exemplo,
    /// o Webhook do Stripe nos notifica com o <c>customer.id</c> que precisa ser salvo para ser usado posteriormente: .
    /// Leia mais em https://stripe.com/docs/billing/subscriptions/build-subscriptions?ui=checkout#create-pricing-model:~:text=To%20determine%20the,database%20for%20verification.
    /// </summary>
    public class StripePaymentInfo
    {
        public StripePaymentInfo(Guid accountId, Account account, string customerId)
        {
            AccountId = accountId;
            Account = account;
            CustomerId = customerId;
        }

        public Guid AccountId { get; set; }
        public Account Account { get; set; }
        public string? CustomerId { get; set; }
    }
}
