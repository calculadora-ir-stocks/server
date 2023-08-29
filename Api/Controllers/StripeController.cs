using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;

namespace Api.Controllers
{
    // TODO [Authorize]
    [Tags("Stripe")]
    public class StripeController : BaseController
    {
        [HttpPost("create-checkout-session")]
        public IActionResult CreateCheckoutSession()
        {
            var domain = "http://localhost:4242";

            var priceOptions = new PriceListOptions
            {
                LookupKeys = new List<string> {
                    Request.Form["lookup_key"]
                }
            };

            var priceService = new PriceService();
            StripeList<Price> prices = priceService.List(priceOptions);

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    Price = prices.Data[0].Id,
                    Quantity = 1,
                  },
                },
                Mode = "subscription",
                SuccessUrl = domain + "?success=true&session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "?canceled=true",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        [HttpPost("create-portal-session")]
        public ActionResult CreatePortalSession()
        {
            // For demonstration purposes, we're using the Checkout session to retrieve the customer ID.
            // Typically this is stored alongside the authenticated user in your database.
            var checkoutService = new SessionService();
            var checkoutSession = checkoutService.Get(Request.Form["session_id"]);

            // This is the URL to which your customer will return after
            // they are done managing billing in the Customer Portal.
            var returnUrl = "http://localhost:4242";

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = checkoutSession.CustomerId,
                ReturnUrl = returnUrl,
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            // Replace this endpoint secret with your endpoint's unique secret
            // If you are testing with the CLI, find the secret by running 'stripe listen'
            // If you are using an endpoint defined with the API or dashboard, look in your webhook settings
            // at https://dashboard.stripe.com/webhooks
            const string endpointSecret = "whsec_12345";

            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);
                var signatureHeader = Request.Headers["Stripe-Signature"];

                stripeEvent = EventUtility.ConstructEvent(json,
                        signatureHeader, endpointSecret);

                if (stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    Console.WriteLine("A subscription was canceled.", subscription.Id);

                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionCanceled(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    Console.WriteLine("A subscription was updated.", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionUpdated(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionCreated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    Console.WriteLine("A subscription was created.", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionUpdated(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionTrialWillEnd)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    Console.WriteLine("A subscription trial will end", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionUpdated(subscription);
                }
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }
                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return BadRequest();
            }
        }
    }
}
