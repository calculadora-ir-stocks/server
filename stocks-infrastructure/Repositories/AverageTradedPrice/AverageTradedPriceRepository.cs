using stocks.Database;
using stocks_core.DTOs.AverageTradedPrice;
using stocks_infrastructure.Enums;
using System.Reflection;

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

        public void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices)
        {
            _context.AverageTradedPrices.AddRange(averageTradedPrices);
            _context.SaveChanges();
        }
    }
}
