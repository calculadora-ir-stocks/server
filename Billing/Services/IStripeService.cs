using Stripe.Checkout;

namespace Billing.Services
{
    public interface IStripeService
    {
        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível analisar todos 
        /// os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        /// <returns>O objeto <c>Session</c> criado do Stripe.</returns>
        Task<Session> CreateCheckoutSession(Guid accountId, string productId);

        /// <summary>
        /// Retorna o plano gratuito para inscrição.
        /// </summary>
        /// <returns>O objeto <c>Session</c> criado do Stripe.</returns>
        Task<Session> CreateCheckoutSessionForFreeTrial(Guid accountId);

        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível gerenciar as formas de pagamento
        /// disponíveis.
        /// </summary>
        /// <param name="checkoutSessionCustomerId">O </param>
        /// <returns>O objeto <c>Session</c> criado do Stripe.</returns>
        Task<Stripe.BillingPortal.Session> CreatePortalSession(Guid accountId);

        Task<Session> GetServiceSessionById(string sessionId);

        /// <summary>
        /// O Stripe envia através de um Webhook atualizações sobre inscrições, cancelamentos e outras informações
        /// sobre planos no geral.
        /// </summary>
        void HandleUserPlansNotifications(string json, string stripeSignatureHeader);
    }
}
