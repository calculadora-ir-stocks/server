namespace Infrastructure.Models
{
    /// <summary>
    /// Quando um usuário assinar um plano, um servidor dedicado do Stripe será criado - o servidor Checkout.
    /// Toda vez que um servidor Checkout é criado, armazenamos na base de dados o pagamento.
    /// Leia mais em https://stripe.com/docs/billing/subscriptions/build-subscriptions?ui=checkout#create-pricing-model:~:text=To%20determine%20the,database%20for%20verification.
    /// </summary>
    public class Order : BaseEntity
    {
        public Order(string customerId, string subscriptionId)
        {
            CustomerId = customerId;
            SubscriptionId = subscriptionId;
        }

        public string CustomerId { get; init; }
        public string SubscriptionId { get; init; }
    }
}
