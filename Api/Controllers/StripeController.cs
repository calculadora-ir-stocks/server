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
        public IActionResult CreateCheckoutSessionForFreeTrial()
        {
            stripeService.CreateCheckoutSessionForFreeTrial();
            return Ok();
        }

        /// <summary>
        /// Retorna os planos e as formas de pagamento disponíveis (o plano gratuito não é retornado).
        /// </summary>
        [HttpPost("create-checkout-session")]
        public IActionResult CreateCheckoutSession()
        {
            Session session = stripeService.CreateCheckoutSession();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        /// <summary>
        /// Possibilita o usuário gerenciar os planos atuais e seus gastos.
        /// </summary>
        /// <returns></returns>
        [HttpPost("create-portal-session")]
        public async Task <IActionResult> CreatePortalSession()
        {
            var session = await stripeService.CreatePortalSession();

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

            await stripeService.HandleUserPlansNotifications(json, stripeSignature);
            return Ok();
        }
    }
}
