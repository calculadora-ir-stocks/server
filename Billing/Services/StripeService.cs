using Common.Constants;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Billing.Services
{
    public class StripeService : IStripeService
    {
        private readonly IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan;
        private readonly IGenericRepository<Order> genericRepositoryStripe;
        private readonly IGenericRepository<Infrastructure.Models.Account> genericRepositoryAccount;

        private readonly ILogger<StripeService> logger;

        // TODO store secret on appSettings.json
        private const string WebhookSecret = "whsec_19efbb10a933ae75a8f1dc3fce9f406e3b206d2df41d81204269373cf56755c5";

        public StripeService(
            IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan,
            IGenericRepository<Order> genericRepositoryStripe,
            IGenericRepository<Infrastructure.Models.Account> genericRepositoryAccount,
            ILogger<StripeService> logger
        )
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
            this.genericRepositoryStripe = genericRepositoryStripe;
            this.genericRepositoryAccount = genericRepositoryAccount;
            this.logger = logger;
        }

        public async Task<Session> CreateCheckoutSession(Guid accountId, string productId)
        {
            var account = genericRepositoryAccount.GetById(accountId);

            // TODO remove it, only for testing purposes
            productId = "price_1NioETElcTcz6jitFPhhg4HH";

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>()
                {
                    new SessionLineItemOptions
                    {
                        Price = productId,
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = "https://localhost:7274/stripe?success=true",
                CancelUrl = "https://localhost:7274/stripe?canceled=true",
                Currency = "BRL",
                Customer = account.StripeCustomerId
            };

            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return session;
            } catch (StripeException s)
            {
                logger.LogError("Ocoprreu um erro ao criar o Checkout Session do Stripe. {e}", s.Message);
                throw new Exception(s.Message);
            }
        }

        public async Task<Session> CreateCheckoutSessionForFreeTrial(Guid accountId)
        {
            var freeTrialPlan = genericRepositoryPlan.GetAll().Where(x => x.Id == PlansConstants.Free).Single();

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions 
                    { 
                        Price = freeTrialPlan.StripeProductId,
                        Quantity = 1 
                    },
                },
                Mode = "subscription",
                SuccessUrl = "https://localhost:7274/stripe?success=true",
                CancelUrl = "https://localhost:7274/stripe?canceled=true",
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
            var session = await service.CreateAsync(options);

            return session;
        }

        public async Task<Stripe.BillingPortal.Session> CreatePortalSession(string checkoutSessionId)
        {
            // TODO: are we going to use bills management?

            var checkoutService = new SessionService();
            var checkoutSession = await checkoutService.GetAsync(checkoutSessionId);

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = checkoutSession.CustomerId,
                ReturnUrl = "This is the URL to which your customer will return after they are done managing billing in the Customer Portal.",
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return session;
        }

        public async Task<Session> GetServiceSessionById(string sessionId)
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            return session;
        }

        public void HandleUserPlansNotifications(string json, string stripeSignatureHeader)
        {
            Event stripeEvent;

            try
            {

                stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, WebhookSecret);

                logger.LogInformation("Webhook do Stripe executado do tipo {stripeEvent.Type} de id {stripeEvent.Id} encontrado.", stripeEvent.Type, stripeEvent.Id);
            }
            catch (Exception e)
            {
                logger.LogError("Something failed {e}", e);
                throw new Exception("Ocorreu um erro ao obter a atualização de plano de um usuário.");
            }

            switch (stripeEvent.Type)
            {
                /**
                 * Webhook event description:
                 * Sent when a customer clicks the Pay or Subscribe button in Checkout, informing you of a new purchase.
                 * */
                case "checkout.session.completed":
                    var session = stripeEvent.Data.Object as Session;

                    bool isOrderPaid = session!.PaymentStatus == "paid";

                    if (isOrderPaid)
                    {
                        genericRepositoryStripe.Add(
                            new Order(session!.CustomerId, session.SubscriptionId)
                        );
                            
                        var options = new SessionGetOptions();
                        options.AddExpand("line_items");

                        var service = new SessionService();

                        Session sessionWithLineItems = service.Get(session.Id, options);
                        StripeList<LineItem> lineItems = sessionWithLineItems.LineItems;

                        SubscribeToPlan(lineItems);
                    }

                    // Payment is successful and the subscription is created.
                    // You should provision the subscription and save the customer ID to your database.
                    break;
                case "invoice.payment_suceeded":
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

        private void SubscribeToPlan(StripeList<LineItem> lineItems)
        {
            throw new NotImplementedException();
        }
    }
}
