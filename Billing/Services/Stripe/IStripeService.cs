using Stripe.Checkout;

namespace Billing.Services.Stripe
{
    public interface IStripeService
    {
        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível analisar todos 
        /// os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        /// <returns>O objeto <c>Session</c> criado do Stripe.</returns>
        Task<Session> CreateCheckoutSession(Guid accountId, string productId, string? couponId = null);

        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível gerenciar as formas de pagamento
        /// disponíveis.
        /// </summary>
        /// <returns>O objeto <c>Session</c> criado do Stripe.</returns>
        Task<Session> CreatePortalSession(Guid accountId);

        /// <summary>
        /// O Stripe envia através de um Webhook atualizações sobre inscrições, cancelamentos e outras informações
        /// sobre planos no geral.
        /// </summary>
        void HandleUserPlansNotifications(string json, string stripeSignatureHeader);
    }
}
