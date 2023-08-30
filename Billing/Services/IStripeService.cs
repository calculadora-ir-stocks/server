using Stripe.Checkout;

namespace Billing.Services
{
    public interface IStripeService
    {
        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível analisar todos 
        /// os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        Task<Session> CreateCheckoutSession(string productId);

        /// <summary>
        /// Retorna o plano gratuito para inscrição.
        /// </summary>
        Task CreateCheckoutSessionForFreeTrial();

        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível gerenciar as formas de pagamento
        /// disponíveis.
        /// </summary>
        /// <param name="checkoutSessionCustomerId">O </param>
        /// <returns></returns>
        Task<Stripe.BillingPortal.Session> CreatePortalSession(string checkoutSessionCustomerId);
        Task<Session> GetServiceSessionById(string sessionId);

        /// <summary>
        /// O Stripe envia através de um Webhook atualizações sobre inscrições, cancelamentos e outras informações
        /// sobre planos no geral.
        /// </summary>
        void HandleUserPlansNotifications(string json, string stripeSignatureHeader);
    }
}
