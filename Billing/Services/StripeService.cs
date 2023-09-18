using Common;
using Common.Constants;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Common.Models;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Billing.Services
{
    public class StripeService : IStripeService
    {
        private readonly IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan;
        private readonly IGenericRepository<Order> genericRepositoryStripe;
        private readonly IAccountRepository accountRepository;

        private readonly StripeWebhookSecret secret;

        private readonly ILogger<StripeService> logger;

        public StripeService(
            IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan,
            IGenericRepository<Order> genericRepositoryStripe,
            IAccountRepository accountRepository,
            IOptions<StripeWebhookSecret> secret,
            ILogger<StripeService> logger
        )
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
            this.genericRepositoryStripe = genericRepositoryStripe;
            this.accountRepository = accountRepository;
            this.secret = secret.Value;
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
            var annualPlan = genericRepositoryPlan.GetAll().Where(x => x.Id == PlansConstants.Anual).Single();

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions 
                    { 
                        Price = annualPlan.StripeProductId,
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
                    Description = "Teste gratuitamente, nada mais justo. Se você achar útil, faça parte do Stocks!"
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

        public void HandleUserPlansNotifications(string json, string stripeSignatureHeader)
        {
            Event stripeEvent;
            Guid sessionId = Guid.NewGuid();            

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, secret.Secret);
            }
            catch (Exception e)
            {
                logger.LogError("Something failed {e}", e);
                throw new Exception("Ocorreu um erro ao obter a atualização de plano de um usuário.");
            }

            logger.LogInformation("Iniciando sessão de Webhook do Stripe de id {sessionId}. " +
                "Objeto Event do Stripe do tipo {type} de id {stripeId}",
                sessionId.ToString(), stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case Events.CheckoutSessionCompleted:
                    /** Sent when the subscription is created. The subscription status might be incomplete if customer authentication is required to
                    complete the payment or if you set payment_behavior to default_incomplete. */

                    FulFillOrder(stripeEvent);
                    break;
                case Events.InvoicePaid:
                    /** Continue to provision the subscription as payments continue to be made.
                    Store the status in your database and check when a user accesses your service.
                    This approach helps you avoid hitting rate limits. */

                    ValidateAccountSubscription(stripeEvent);
                    break;
                case Events.CustomerSubscriptionDeleted:
                    /** Sent when a customer’s subscription ends. */

                    ExpiresAccountSubscription(stripeEvent);
                    break;
                case "invoice.payment_failed":
                    PausesAccountSubscription(stripeEvent);
                    break;
                default:
                    break;
            }
        }

        private void PausesAccountSubscription(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Invoice;

            var account = accountRepository.GetByStripeCustomerId(subscription!.CustomerId);

            if (FailedPaymentIsFromExistingPlan(subscription))
            {
                account.Status = EnumHelper.GetEnumDescription(AccountStatus.SubscriptionPaused);
                accountRepository.Update(account);

                logger.LogInformation("Usuário de id {id} teve um débito inválido e o plano agora está pausado.", account.Id);
            }
        }

        /// <summary>
        /// Há alguns cenários que ativam o Webhook de parâmetro <c>invoice_payment_failed</c>.
        /// Um deles é quando um usuário vai comprar um plano e o cartão falha.
        /// O outro é quando um usuário já possui um plano salvo e, na hora de debitar, o cartão falha.
        /// 
        /// Para pausar a inscrição de um usuário, o pagamento falho deve ser de uma inscrição já existente.
        /// </summary>
        private static bool FailedPaymentIsFromExistingPlan(Invoice subscription)
        {
            return subscription.BillingReason == "subscription_cycle";
        }

        private void ValidateAccountSubscription(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            var account = accountRepository.GetByStripeCustomerId(invoice!.CustomerId);

            account.Status = EnumHelper.GetEnumDescription(AccountStatus.SubscriptionValid);
            accountRepository.Update(account);

            logger.LogInformation("Usuário de id {id} teve um débito válido e o plano continua sendo válido.", account.Id);
        }

        private void ExpiresAccountSubscription(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Subscription;
            var account = accountRepository.GetByStripeCustomerId(subscription!.CustomerId);

            account.Status = EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired);
            accountRepository.Update(account);

            logger.LogInformation("Usuário de id {id} teve o seu plano expirado.", account.Id);
        }

        private void FulFillOrder(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;

            bool isOrderPaid = session!.PaymentStatus == "paid";

            if (isOrderPaid)
            {
                genericRepositoryStripe.Add(
                    new Order(session.CustomerId, session.SubscriptionId)
                );

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
                account.Status = EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.SubscriptionValid);

                accountRepository.Update(account);

                logger.LogInformation("Usuário de id {id} comprou o plano de id {planId}.", account.Id, planId);
            }
            else
            {
                // fuck you then
            }
        }
    }
}
