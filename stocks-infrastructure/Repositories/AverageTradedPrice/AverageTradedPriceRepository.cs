using Dapper;
using Microsoft.EntityFrameworkCore;
using stocks.Database;
using stocks_common.Models;
using stocks_infrastructure.Dtos;
using stocks_infrastructure.Models;

namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public class AverageTradedPriceRepository : IAverageTradedPriceRepostory
    {
        private readonly StocksContext context;

        public AverageTradedPriceRepository(StocksContext context)
        {
            this.context = context;
        }

        #region INSERT
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

        public async Task AddAllAsync(List<Models.AverageTradedPrice> averageTradedPrices)
        {
            context.AddRange(averageTradedPrices);

            context.AttachRange(averageTradedPrices.Select(x => x.Account));

            await context.SaveChangesAsync();
        }
        #endregion

        #region UPDATE
        public async Task UpdateAllAsync(List<Models.AverageTradedPrice> averageTradedPrices)
        {
            context.AverageTradedPrices.UpdateRange(averageTradedPrices);

            context.AttachRange(averageTradedPrices.Select(x => x.Account));

            await context.SaveChangesAsync();
        }
        #endregion

        #region GET
        public async Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPrices(Guid accountId, List<string>? tickers = null)
        {
            DynamicParameters parameters = new();

            parameters.Add("@AccountId", accountId);
            parameters.Add("@Tickers", tickers);

            string sql =
                @"SELECT 
                    atp.""Ticker"",
                    atp.""AveragePrice"" as AverageTradedPrice,
                    atp.""Quantity""
                  FROM ""AverageTradedPrices"" atp
                  WHERE atp.""AccountId"" = @AccountId AND
                  atp.""Ticker"" = ANY(@Tickers);
                ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);

            return response;
        }
        public bool AlreadyHasAverageTradedPrice(Guid accountId)
        {
            return context.AverageTradedPrices.Where(x => x.Account.Id.Equals(accountId)).FirstOrDefault() != null;
        }

        public Models.AverageTradedPrice? GetAverageTradedPrice(string ticker, Guid accountId)
        {
            return context.AverageTradedPrices.Where(x => x.Ticker == ticker && x.Account.Id == accountId).FirstOrDefault();
        }
        #endregion

        #region DELETE
        public async Task RemoveAllAsync(IEnumerable<string?> tickers, Guid id)
        {
            DynamicParameters parameters = new();

            parameters.Add("@AccountId", id);
            parameters.Add("@Tickers", tickers.ToList());

            string sql =
                @"DELETE 
                    FROM ""AverageTradedPrices"" atp
                  WHERE atp.""AccountId"" = @AccountId AND
                  atp.""Ticker"" = ANY(@Tickers);
                ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);
        }
        #endregion
    }
}
