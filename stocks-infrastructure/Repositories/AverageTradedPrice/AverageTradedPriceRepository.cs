using stocks.Database;

namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public class AverageTradedPriceRepository : IAverageTradedPriceRepository
    {
        private readonly StocksContext _context;

        public AverageTradedPriceRepository(StocksContext context)
        {
            _context = context;
        }

        public bool AccountAlreadyHasAverageTradedPrice(Guid accountId)
        {
            return _context.AverageTradedPrices.Where(x => x.AccountId.Equals(accountId)).FirstOrDefault() != null;
        }
    }
}
