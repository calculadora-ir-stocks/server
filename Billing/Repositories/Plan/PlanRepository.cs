using Api.Database;
using Billing.Dtos;
using Stripe;

namespace Infrastructure.Repositories.Plan
{
    public class PlanRepository : IPlanRepository
    {
        private readonly StocksContext context;

        public PlanRepository(StocksContext context)
        {
            this.context = context;
        }

        public IEnumerable<Models.Plan> GetAllAccountPlans()
        {
            return context.Plans.ToList();
        }

        public IEnumerable<StripePlanDto> GetAllStripePlans()
        {
            var productOptions = new ProductListOptions();
            var priceOptions = new PriceListOptions();

            var productService = new ProductService();
            var priceService = new PriceService();

            StripeList<Product> products = productService.List(productOptions);
            StripeList<Price> prices = priceService.List(priceOptions);

            foreach (var product in products.Data.Where(x => x.Active))
            {
                var price = prices.Where(x => x.Active && x.ProductId == product.Id).First();

                yield return new StripePlanDto
                (
                    price.Id,
                    product.Name,
                    price.UnitAmount // Conversão de centavos para real
                );
            }
        }

        public Models.Plan GetByAccountId(Guid accountId)
        {
            return context.Plans.Where(x => x.AccountId == accountId).First();
        }

        public void Update(Models.Plan plan)
        {
            context.Plans.Update(plan);
            context.SaveChanges();
        }
    }
}
