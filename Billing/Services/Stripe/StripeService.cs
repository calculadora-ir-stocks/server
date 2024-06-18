using Billing.Dtos;
using Common.Constants;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Common.Options;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.Plan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Billing.Services.Stripe
{
    public class StripeService : IStripeService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IPlanRepository planRepository;

        private readonly IOptions<StripeOptions> options;

        private readonly ILogger<StripeService> logger;

        public StripeService(
            IAccountRepository accountRepository,
            IPlanRepository planRepository,
            IOptions<StripeOptions> options,
            ILogger<StripeService> logger
        )
        {
            this.accountRepository = accountRepository;
            this.planRepository = planRepository;
            this.options = options;
            this.logger = logger;
        }

        public async Task<Session> CreateCheckoutSession(Guid accountId, string productId, string? couponId = null)
        {
            var account = await accountRepository.GetById(accountId) ?? throw new NotFoundException("Investidor", accountId.ToString());

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
            }
            catch (StripeException s)
            {
                logger.LogError("Ocorreu um erro ao criar o Checkout Session do Stripe. {e}", s.Message);
                throw new Exception(s.Message);
            }
        }

        public async Task<Session> CreatePortalSession(Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void HandleUserPlansNotifications(string json, string stripeSignatureHeader)
        {
            Event stripeEvent;
            Guid sessionId = Guid.NewGuid();

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, options.Value.WebhookToken, throwOnApiVersionMismatch: false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao deserializar um evento do Stripe.");
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
            }
        }

        private void FulFillOrder(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;

            bool isOrderPaid = session!.PaymentStatus == "paid";

            if (isOrderPaid)
            {
                var (subscribedPlan, expiresAt) = GetSubscribedPlan(session.AmountSubtotal);

                var account = accountRepository.GetByStripeCustomerId(session.CustomerId);
                account.Status = AccountStatus.SubscriptionValid.GetEnumDescription();

                var plan = planRepository.GetByAccountId(account.Id);

                plan.Name = subscribedPlan.Name;
                plan.ExpiresAt = expiresAt;

                planRepository.Update(plan, account);

                logger.LogInformation("Usuário de id {id} acabou de assinar um plano. We getting rich baby!", account.Id);
            }
            else
            {
                // fuck you then
            }
        }

        private (StripePlanDto, DateTime) GetSubscribedPlan(long? amountSubtotal)
        {
            // TODO O Stripe - serviço de horrenda documentação - não retorna o id do produto que o usuário
            // comprou na sessão de Checkout. Dessa forma, o plano é assimilado através do preço.
            // Por isso, por enquanto, dois planos não podem ter preços iguais.
            var plans = planRepository.GetAllStripePlans();

            var plan = plans.Where(x => x.Price == amountSubtotal).First();
            DateTime expiresAt = DateTime.Now;

            switch (plan.Name)
            {
                case PlansConstants.Anual:
                    expiresAt = expiresAt.AddYears(1);
                    break;
                case PlansConstants.Semester:
                    expiresAt = expiresAt.AddMonths(6);
                    break;
                case PlansConstants.Monthly:
                    expiresAt = expiresAt.AddMonths(1);
                    break;
            }

            return (plan, expiresAt);
        }
    }
}
