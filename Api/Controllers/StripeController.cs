using Billing.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace Api.Controllers
{
    // TODO [Authorize]
    [Tags("Stripe")]
    public class StripeController : BaseController
    {
        private readonly IStripeService stripeService;

        public StripeController(IStripeService stripeService)
        {
            this.stripeService = stripeService;
        }

        /// <summary>
        /// Retorna o plano gratuito sem nenhuma forma de pagamento vinculada.
        /// Deve ser retornado no onboarding do usuário na plataforma.
        /// </summary>
        /// <param name="accountId">O id da conta do usuário que está criando o Checkout do Stripe.</param>
        /// <returns>Objeto <c>Session</c> da sessão de Checkout criada e no Header <c>Location</c> a URL
        /// do Checkout que o usuário deve ser redirecionado.</returns>
        [HttpPost("create-checkout-session/free-trial/{accountId}")]
        public async Task<IActionResult> CreateCheckoutSessionForFreeTrial([FromRoute] Guid accountId)
        {
            var session = await stripeService.CreateCheckoutSessionForFreeTrial(accountId);

            Response.Headers.Add("Location", session.Url);
            return Ok(session);
        }

        /// <summary>
        /// Retorna os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        /// <param name="productId">O id do produto vinculado ao Stripe.</param>
        /// <param name="accountId">O id da conta do usuário que está criando o Checkout do Stripe.</param>
        /// <returns>Objeto <c>Session</c> da sessão de Checkout criada e no Header <c>Location</c> a URL
        /// do Checkout que o usuário deve ser redirecionado.</returns>
        [HttpPost("create-checkout-session/{productId}/{accountId}")]
        public async Task <IActionResult> CreateCheckoutSession([FromRoute] Guid accountId, string productId)
        {
            Session session = await stripeService.CreateCheckoutSession(accountId, productId);

            Response.Headers.Add("Location", session.Url);
            return Ok(session);
        }

        [HttpGet("checkout-session")]
        public async Task<IActionResult> CheckoutSession(string sessionId)
        {
            Session session = await stripeService.GetServiceSessionById(sessionId);
            return Ok(session);
        }

        /// <summary>
        /// Cria um Portal Session do Stripe onde é possível gerenciar todas as formas de pagamento
        /// salvas (se alguma existir).
        /// </summary>
        /// <param name="accountId">O id da conta do usuário que está criando o Portal Session do Stripe.</param>
        /// <returns>Objeto <c>Session</c> da sessão criada e no Header <c>Location</c> a URL
        /// do Portal Session que o usuário deve ser redirecionado.</returns>
        [HttpPost("create-portal-session/{accountId}")]
        public async Task <IActionResult> CreatePortalSession([FromRoute] Guid accountId)
        {
            var session = await stripeService.CreatePortalSession(accountId);

            Response.Headers.Add("Location", session.Url);
            return Ok(session);
        }

        /// <summary>
        /// Endpoint utilizado para a comunicação com Webhooks do Stripe. Não deve ser utilizado internamente.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            string? json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string? stripeSignature = Request.Headers["Stripe-Signature"];

            if (json is null || stripeSignature is null)
            {
                return BadRequest("É necessário enviar o JSON no body da requisição e a assinatura digital" +
                    "do Stripe no Header Stripe-Signature.");
            }

            stripeService.HandleUserPlansNotifications(json, stripeSignature);
            return Ok();
        }
    }
}
