using Dapper;
using Microsoft.EntityFrameworkCore;
using Api.Database;
using Infrastructure.Dtos;

namespace Infrastructure.Repositories.AverageTradedPrice
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
            await context.SaveChangesAsync();
        }
        #endregion

        #region GET
        public async Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPricesDto(Guid accountId, List<string>? tickers = null)
        {
            DynamicParameters parameters = new();

            parameters.Add("@AccountId", accountId);
            parameters.Add("@Tickers", tickers);

            string sql =
                @"SELECT 
                    atp.""Ticker"",
                    atp.""AveragePrice"" as AverageTradedPrice,
                    atp.""TotalBought"" as TotalBought,
                    atp.""Quantity""
                  FROM ""AverageTradedPrices"" atp
                  WHERE atp.""AccountId"" = @AccountId AND
                  atp.""Ticker"" = ANY(@Tickers);
                ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);

            return response;
        }

        public List<Models.AverageTradedPrice>? GetAverageTradedPrices(Guid accountId, List<string>? tickers = null)
        {
            if (tickers is null) return context.AverageTradedPrices.Where(x => x.Account.Id == accountId).ToList();
            return context.AverageTradedPrices.Where(x => tickers.Contains(x!.Ticker) && x.Account.Id.Equals(accountId)).ToList();
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
            await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);
        }
        #endregion
    }
}
