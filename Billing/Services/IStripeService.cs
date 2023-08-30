using Stripe.Checkout;

namespace Billing.Services
{
    public interface IStripeService
    {
        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível analisar todos 
        /// os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        Session CreateCheckoutSession();

        /// <summary>
        /// Retorna o plano gratuito para inscrição.
        /// </summary>
        void CreateCheckoutSessionForFreeTrial();

        /// <summary>
        /// Cria um servidor dedicado do Stripe onde é possível gerenciar as formas de pagamento
        /// disponíveis.
        /// </summary>
        /// <param name="checkoutSessionCustomerId">O </param>
        /// <returns></returns>
        Task<Stripe.BillingPortal.Session> CreatePortalSession(string checkoutSessionCustomerId);

        /// <summary>
        /// O Stripe envia através de um Webhook atualizações sobre inscrições, cancelamentos e outras informações
        /// sobre planos no geral.
        /// </summary>
        Task HandleUserPlansNotifications(string json, string stripeSignatureHeader);
    }
}
