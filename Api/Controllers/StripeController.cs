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

        [HttpPost("create-checkout-session/free-trial")]
        public async Task<IActionResult> CreateCheckoutSessionForFreeTrial()
        {
            await stripeService.CreateCheckoutSessionForFreeTrial();
            return StatusCode(303);
        }

        /// <summary>
        /// Retorna os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        /// <param name="productId">O id do produto vinculado ao Stripe.</param>
        /// <returns><c>CustomerId da sessão de Checkout criada.</c></returns>
        [HttpPost("create-checkout-session")]
        public async Task <IActionResult> CreateCheckoutSession(string productId)
        {
            Session session = await stripeService.CreateCheckoutSession(productId);

            Response.Headers.Add("Location", session.Url);
            return Ok(session.Url);
        }

        [HttpGet("checkout-session")]
        public async Task<IActionResult> CheckoutSession(string sessionId)
        {
            Session session = await stripeService.GetServiceSessionById(sessionId);
            return Ok(session);
        }

        /// <summary>
        /// Possibilita o usuário gerenciar os planos atuais e seus gastos.
        /// </summary>
        /// <returns></returns>
        [HttpPost("create-portal-session")]
        public async Task <IActionResult> CreatePortalSession([FromRoute] string checkoutSessionCustomerId)
        {
            var session = await stripeService.CreatePortalSession(checkoutSessionCustomerId);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

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
