using stocks.Database;

namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public class AverageTradedPriceRepository : IAverageTradedPriceRepostory
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

        public Models.AverageTradedPrice GetAverageTradedPrice(string ticker, Guid accountId)
        {
            return _context.AverageTradedPrices.Where(x => x.Ticker == ticker && x.AccountId == accountId).First();
        }

        public void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices)
        {
            _context.AverageTradedPrices.AddRange(averageTradedPrices);
            _context.SaveChanges();
        }

        public void Update(Guid id, string ticker)
        {
            DateTime lastUpdated = 
                _context.AverageTradedPrices.Where(x => x.AccountId == id && x.Ticker == ticker).First().UpdatedAt;

            string lastUpdatedFormatted = lastUpdated.ToString("yyyy-MM-dd");
            string referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");


        }
    }
}
