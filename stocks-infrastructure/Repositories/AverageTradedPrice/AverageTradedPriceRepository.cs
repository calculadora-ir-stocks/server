using stocks.Database;
using Dapper;
using Microsoft.EntityFrameworkCore;
using stocks_infrastructure.Models;
using stocks_infrastructure.Dtos;

namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public class AverageTradedPriceRepository : IAverageTradedPriceRepostory
    {
        private readonly StocksContext context;

        public AverageTradedPriceRepository(StocksContext context)
        {
            this.context = context;
        }

        public bool AlreadyHasAverageTradedPrice(Guid accountId)
        {
            return context.AverageTradedPrices.Where(x => x.Account.Id.Equals(accountId)).FirstOrDefault() != null;
        }

        public async Task AddAllAsync(List<Models.AverageTradedPrice> averageTradedPrices)
        {
            context.AddRange(averageTradedPrices);

            context.AttachRange(averageTradedPrices.Select(x => x.Account));

            await context.SaveChangesAsync();
        }

        public Models.AverageTradedPrice? GetAverageTradedPrice(string ticker, Guid accountId)
        {
            return context.AverageTradedPrices.Where(x => x.Ticker == ticker && x.Account.Id == accountId).FirstOrDefault();
        }

        public async Task Insert(Models.AverageTradedPrice averageTradedPrice)
        {
            await context.AverageTradedPrices.AddAsync(averageTradedPrice);
            context.SaveChanges();
        }

        public void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices)
        {
            context.AverageTradedPrices.AddRange(averageTradedPrices);
            context.SaveChanges();
        }

        public void Update(Guid id, string ticker)
        {
            DateTime lastUpdated = 
                context.AverageTradedPrices.Where(x => x.Account.Id == id && x.Ticker == ticker).First().UpdatedAt;

            string lastUpdatedFormatted = lastUpdated.ToString("yyyy-MM-dd");
            string referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        }

        public async Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPrices(Guid accountId, List<string>? tickers = null)
        {
            DynamicParameters parameters = new();

            parameters.Add("@AccountId", accountId);
            parameters.Add("@Tickers", tickers);

            string sql =
                @"SELECT 
                    atp.""Ticker"",
                    atp.""AveragePrice"",
                    atp.""Quantity""
                  FROM ""AverageTradedPrices"" atp
                  WHERE atp.""AccountId"" = @AccountId AND
                  atp.""Ticker"" = ANY(@Tickers);
                ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);

            return response;
        }
    }
}
