
using Api.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.BonusShare
{
    public class BonusShareRepository : IBonusShareRepository
    {
        private readonly StocksContext dbContext;

        public BonusShareRepository(StocksContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<Models.BonusShare?> GetByTickerAndDate(string ticker, DateTime date)
        {
            return await dbContext.BonusShares.Where(x => x.Ticker.Equals(ticker) & x.Date.Equals(date.ToString("yyyy-MM-dd"))).AsNoTracking().FirstOrDefaultAsync();
        }
    }
}