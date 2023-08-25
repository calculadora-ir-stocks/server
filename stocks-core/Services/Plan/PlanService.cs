using stocks.Repositories;
using stocks_core.Models.Requests.Plan;
using stocks_core.Models.Stripe;
using Stripe;

namespace stocks_core.Services.Plan
{
    public class PlanService : IPlanService
    {
        private readonly IGenericRepository<stocks_infrastructure.Models.Plan> genericRepositoryPlan;
        private readonly IGenericRepository<stocks_infrastructure.Models.Account> genericRepositoryAccount;

        private readonly ChargeService chargeService;
        private readonly CustomerService customerService;
         
        private readonly TokenService tokenService;

        public PlanService(
            IGenericRepository<stocks_infrastructure.Models.Plan> genericRepositoryPlan,
            IGenericRepository<stocks_infrastructure.Models.Account> genericRepositoryAccount,
            ChargeService chargeService,
            CustomerService customerService,
            TokenService tokenService
        )
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
            this.genericRepositoryAccount = genericRepositoryAccount;
            this.chargeService = chargeService;
            this.customerService = customerService;
            this.tokenService = tokenService;
        }

        public IEnumerable<stocks_infrastructure.Models.Plan> GetAll()
        {
            return genericRepositoryPlan.GetAll();
        }

        public async void Subscribe(PlanSubscribeRequest request, CancellationToken cancellationToken)
        {
            var plan = genericRepositoryPlan.GetById(request.PlanId);
            if (plan is null) throw new Exception($"O plano de id {request.PlanId} não foi encontrado em nossa base de dados.");

            var account = genericRepositoryAccount.GetById(request.AccountId);
            if (account is null) throw new Exception($"A conta de id {request.AccountId} não foi encontrado em nossa base de dados.");

            try
            {
                await AddStripeCustomerAsync(
                    new AddStripeCustomer(account.Email, account.Name, request.CreditCardInformation),
                    cancellationToken
                );

                await AddStripePaymentAsync(
                    new AddStripePayment(account.Id.ToString(), account.Email, plan.Description, plan.Price),
                    cancellationToken
                );
            }
            catch
            {

            }
        }

        private async Task<StripeCustomer> AddStripeCustomerAsync(AddStripeCustomer customer, CancellationToken ct)
        {
            TokenCreateOptions tokenOptions = new()
            {
                Card = new TokenCardOptions
                {
                    Name = customer.Name,
                    Number = customer.CreditCard.CardNumber,
                    ExpYear = customer.CreditCard.ExpirationYear,
                    ExpMonth = customer.CreditCard.ExpirationMonth,
                    Cvc = customer.CreditCard.Cvc
                }
            };

            Token stripeToken = await tokenService.CreateAsync(tokenOptions, null, ct);

            CustomerCreateOptions customerOptions = new()
            {
                Name = customer.Name,
                Email = customer.Email,
                Source = stripeToken.Id
            };

            Customer createdCustomer = await customerService.CreateAsync(customerOptions, null, ct);

            return new StripeCustomer(createdCustomer.Name, createdCustomer.Email, createdCustomer.Id);
        }

        private async Task<StripePayment> AddStripePaymentAsync(AddStripePayment payment, CancellationToken ct)
        {
            ChargeCreateOptions paymentOptions = new()
            {
                Customer = payment.CustomerId,
                ReceiptEmail = payment.ReceiptEmail,
                Description = payment.Description,
                Currency = "BRL",
                Amount = payment.Amount
            };

            var createdPayment = await chargeService.CreateAsync(paymentOptions, null, ct);

            return new StripePayment(
              createdPayment.CustomerId,
              createdPayment.ReceiptEmail,
              createdPayment.Description,
              createdPayment.Currency,
              createdPayment.Amount,
              createdPayment.Id
            );
        }

    }
}
