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

        public async Task UpdateTickers(Guid id, IEnumerable<AverageTradedPriceDetails> tickers)
        {
            DynamicParameters parameters = new();

            parameters.Add("@AccountId", id);
            parameters.Add("@Tickers", tickers.Select(x => x.TickerSymbol));
            parameters.Add("@Prices", tickers.Select(x => x.AverageTradedPrice));
            parameters.Add("@Quantities", tickers.Select(x => x.TradedQuantity));

            string sql =
                @"UPDATE ""AverageTradedPrices""
                    SET
                        ""AveragePrice"" = 
                            CASE 
                                WHEN (""Ticker"" IN (@Tickers)) THEN @Prices
                                ELSE ""AveragePrice""
                            END,
                        ""Quantity"" =
                            CASE
                                WHEN(""Ticker"" IN (@Tickers) THEN @Quantities
                                ELSE ""Quantity""
                            END
                    WHERE ""AccountId"" = @AccountId;
                ";

            var connection = context.Database.GetDbConnection();
            await connection.QueryAsync(sql, parameters);
        }

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
    }
}
