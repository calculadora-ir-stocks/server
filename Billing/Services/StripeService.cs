using Common.Constants;
using Common.Exceptions;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Billing.Services
{
    public class StripeService : IStripeService
    {
        private readonly IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan;
        private readonly IGenericRepository<Order> genericRepositoryStripe;
        private readonly IAccountRepository accountRepository;

        private readonly ILogger<StripeService> logger;

        // TODO store secret on appSettings.json
        private const string WebhookSecret = "whsec_19efbb10a933ae75a8f1dc3fce9f406e3b206d2df41d81204269373cf56755c5";

        public StripeService(
            IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan,
            IGenericRepository<Order> genericRepositoryStripe,
            IAccountRepository accountRepository,
            ILogger<StripeService> logger
        )
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
            this.genericRepositoryStripe = genericRepositoryStripe;
            this.accountRepository = accountRepository;
            this.logger = logger;
        }

        public async Task<Session> CreateCheckoutSession(Guid accountId, string productId, string? couponId = null)
        {
            var account = accountRepository.GetById(accountId);
            if (account is null) throw new RecordNotFoundException("Investidor", accountId.ToString());

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
                Mode = "subscription",
                SuccessUrl = "https://localhost:7274/stripe?success=true",
                CancelUrl = "https://localhost:7274/stripe?canceled=true",
                Currency = "BRL",
                Customer = account.StripeCustomerId
            };

            if (couponId is not null)
            {
                options.Discounts = new List<SessionDiscountOptions>
                {
                    new SessionDiscountOptions
                    {
                        Coupon = couponId,
                    }
                };
            }

            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return session;
            } catch (StripeException s)
            {
                logger.LogError("Ocorreu um erro ao criar o Checkout Session do Stripe. {e}", s.Message);
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

        public async Task<Stripe.BillingPortal.Session> CreatePortalSession(Guid accountId)
        {
            var account = accountRepository.GetById(accountId);
            if (account is null) throw new RecordNotFoundException("Investidor", accountId.ToString());

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = account.StripeCustomerId,
                ReturnUrl = "https://localhost:7274/stripe?returned=true",
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
                case Events.CheckoutSessionCompleted:
                    var session = stripeEvent.Data.Object as Session;

                    bool isOrderPaid = session!.PaymentStatus == "paid";

                    if (isOrderPaid)
                    {
                        genericRepositoryStripe.Add(
                            new Order(session.CustomerId, session.SubscriptionId)
                        );

                        FulFillOrder(session);
                    }
                    else
                    {
                        // fuck you then
                    }

                    break;
                case Events.CustomerSubscriptionDeleted:
                    var subscription = stripeEvent.Data.Object as Subscription;

                    ExpiresPlan(subscription!);
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
            }
        }

        private void ExpiresPlan(Subscription subscription)
        {
            var account = accountRepository.GetByStripeCustomerId(subscription.CustomerId);

            account.IsPlanExpired = true;
            accountRepository.Update(account);
        }

        private void FulFillOrder(Session session)
        {            
            var options = new SessionGetOptions();
            options.AddExpand("line_items");

            var service = new SessionService();

            Session sessionWithLineItems = service.Get(session.Id, options);

            var lineItem = sessionWithLineItems.LineItems.Data.First();
            string productId = lineItem!.Price.Id;

            int planId = genericRepositoryPlan
                .GetAll()
                .Where(x => x.StripeProductId == productId)
                .Select(x => x.Id)
                .First();

            var account = accountRepository.GetByStripeCustomerId(session.CustomerId);

            account.PlanId = planId;
            account.IsPlanExpired = false;

            accountRepository.Update(account);
        }
    }
}
