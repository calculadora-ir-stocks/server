using Common.Constants;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;

namespace Billing.Services
{
    public class StripeService : IStripeService
    {
        private readonly IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan;
        private readonly IGenericRepository<StripePaymentInfo> genericRepositoryStripe;

        public StripeService(IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan, IGenericRepository<StripePaymentInfo> genericRepositoryStripe)
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
            this.genericRepositoryStripe = genericRepositoryStripe;
        }

        public Session CreateCheckoutSession()
        {
            var plans = genericRepositoryPlan.GetAll().Where(x => x.Id != PlansConstants.Free);

            var items = new List<SessionLineItemOptions>();

            foreach (var plan in plans)
            {
                items.Add(new SessionLineItemOptions { Price = plan.StripeProductId, Quantity = 1 });
            }

            var options = new SessionCreateOptions
            {
                LineItems = items,
                Mode = "subscription",
                SuccessUrl = "domain" + "?success=true&session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "domain" + "?canceled=true",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return session;
        }

        public void CreateCheckoutSessionForFreeTrial()
        {
            var freeTrialPlan = genericRepositoryPlan.GetAll().Where(x => x.Id == PlansConstants.Free).Single();

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = "domain" + "?success=true&session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "domain" + "?canceled=true",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = freeTrialPlan.StripeProductId, Quantity = 1 },
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialSettings = new SessionSubscriptionDataTrialSettingsOptions
                    {
                        EndBehavior = new SessionSubscriptionDataTrialSettingsEndBehaviorOptions
                        {
                            MissingPaymentMethod = "cancel",
                        },
                    },
                    TrialPeriodDays = 30,
                },
                PaymentMethodCollection = "if_required",
            };

            var service = new SessionService();
            service.Create(options);
        }

        public async Task<Stripe.BillingPortal.Session> CreatePortalSession(string checkoutSessionCustomerId)
        {
            string customerId = "Here CUSTOMER_ID refers to the customer ID created by a Checkout Session that you saved while" +
                " processing the checkout.session.completed webhook. You can also set a default redirect link for the portal in the Dashboard.\r\n\r\n";

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = checkoutSessionCustomerId,
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return session;
        }

        public async Task HandleUserPlansNotifications(string json, string stripeSignatureHeader)
        {
            Event stripeEvent;

            try
            {
                string webhookSecret = "STRIPE_WEBHOOK_SECRET";

                stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, webhookSecret);

                Console.WriteLine($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something failed {e}");
                throw new Exception("Ocorreu um erro ao obter a atualização de plano de um usuário.");
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    string? stripeObject = stripeEvent.Data.Object.ToString();

                    if (stripeObject is null) throw new Exception("O objeto de evento de Webhook do Stripe é nulo.");

                    // StripeEventId stripeEventCustomerId = JsonConvert.DeserializeObject<StripeEventId>(stripeObject!);
                    // genericRepositoryStripe.Add(new StripePaymentInfo(stripeEventCustomerId.Id));

                    // Sent when a customer clicks the Pay or Subscribe button in Checkout, informing you of a new purchase.

                    // Payment is successful and the subscription is created.
                    // You should provision the subscription and save the customer ID to your database.
                    break;
                case "invoice.paid":
                    // Should we use this without automatic payment?
                    // Sent each billing interval when a payment succeeds.

                    // Continue to provision the subscription as payments continue to be made.
                    // Store the status in your database and check when a user accesses your service.
                    // This approach helps you avoid hitting rate limits.
                    break;
                case "invoice.payment_failed":
                    // Sent each billing interval if there is an issue with your customer’s payment method.

                    // The payment failed or the customer does not have a valid payment method.
                    // The subscription becomes past_due. Notify your customer and send them to the
                    // customer portal to update their payment information.
                    break;
                case "customer.subscription.trial_will_end":
                    // https://stripe.com/docs/payments/checkout/free-trials#customer-portal:~:text=You%20can%20also%20send%20the%20reminder%20email%20yourself%2C%20and%20redirect%20customers%20to%20the%20Billing%20customer%20portal%20to%20add%20their%20payment%20details.
                    break;
                default:
                    break;
                    // Unhandled event type
            }
        }
    }
}
