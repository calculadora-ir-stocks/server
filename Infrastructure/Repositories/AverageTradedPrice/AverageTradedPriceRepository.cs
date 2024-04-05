using Api.Database;
using Common;
using Common.Constants;
using Dapper;
using Infrastructure.Dtos;
using Microsoft.EntityFrameworkCore;

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
        public async Task AddAsync(Models.AverageTradedPrice averageTradedPrice)
        {
            DynamicParameters parameters = new();

            const string key = "GET THIS SHIT FROM A HSM";

            parameters.Add("@Key", key);
            parameters.Add("@Ticker", averageTradedPrice.Ticker);
            parameters.Add("@AveragePrice", averageTradedPrice.AveragePrice);
            parameters.Add("@TotalBought", averageTradedPrice.TotalBought);
            parameters.Add("@Quantity", averageTradedPrice.Quantity);
            parameters.Add("@AccountId", averageTradedPrice.Account.Id);

            string sql = @"
                INSERT INTO ""AverageTradedPrices""
                (
	                ""Id"",
	                ""Ticker"",
	                ""AveragePrice"",
	                ""TotalBought"",
	                ""Quantity"",
	                ""AccountId"",
	                ""UpdatedAt""
                )
                VALUES
                (
	                gen_random_uuid(),
                    PGP_SYM_ENCRYPT(@Ticker, @Key),
                    PGP_SYM_ENCRYPT(@AveragePrice, @Key),
                    PGP_SYM_ENCRYPT(@TotalBought, @Key),
                    PGP_SYM_ENCRYPT(@Quantity, @Key),
                    @AccountId,
                    NOW()
                )
            ";

            await context.Database.GetDbConnection().QueryAsync(sql, parameters);
            Auditor.Audit($"{nameof(AverageTradedPrice)}:{AuditOperation.Add}", comment: "Neste evento todas as informações de preços médios foram criptografadas na base de dados.");
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

            const string key = "GET THIS SHIT FROM A HSM";

            parameters.Add("@AccountId", accountId);
            parameters.Add("@Tickers", tickers);
            parameters.Add("@Key", key);

            string sql =
                @"SELECT 
                    PGP_SYM_DECRYPT(atp.""Ticker""::bytea, @Key) as Ticker,
                    CAST(PGP_SYM_DECRYPT(atp.""AveragePrice""::bytea, @Key) as double precision) as AverageTradedPrice,
                    CAST(PGP_SYM_DECRYPT(atp.""TotalBought""::bytea, @Key) as double precision) as TotalBought,
                    CAST(PGP_SYM_DECRYPT(atp.""Quantity""::bytea, @Key) as double precision) as Quantity
                  FROM ""AverageTradedPrices"" atp
                  WHERE atp.""AccountId"" = @AccountId AND
                  PGP_SYM_DECRYPT(atp.""Ticker""::bytea, @Key) = ANY(@Tickers);
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
                    PGP_SYM_DECRYPT(atp.""Ticker""::bytea, @Key) = ANY(@Tickers);
                ";

            var connection = context.Database.GetDbConnection();
            await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);
        }
        #endregion
    }
}
