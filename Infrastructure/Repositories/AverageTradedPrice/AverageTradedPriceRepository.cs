using Api.Database;
using Common;
using Common.Constants;
using Common.Options;
using Dapper;
using Infrastructure.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Repositories.AverageTradedPrice
{
    public class AverageTradedPriceRepository : IAverageTradedPriceRepostory
    {
        private readonly StocksContext context;
        private readonly IOptions<DatabaseEncryptionKeyOptions> key;

        public AverageTradedPriceRepository(StocksContext context, IOptions<DatabaseEncryptionKeyOptions> key)
        {
            this.context = context;
            this.key = key;
        }

        #region INSERT
        public async Task AddAsync(Models.AverageTradedPrice averageTradedPrice)
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

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
            Auditor.Audit($"{nameof(AverageTradedPrice)}:{AuditOperation.Add}", comment: "Todas as informações de preços médios foram criptografadas na base de dados.");
        }
        #endregion

        #region GET
        public async Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPrices(Guid accountId, IEnumerable<string>? tickers = null)
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);
            parameters.Add("@AccountId", accountId);
            parameters.Add("@Tickers", tickers);

            string sql =
                @"SELECT 
                    PGP_SYM_DECRYPT(atp.""Ticker""::bytea, @Key) as Ticker,
                    CAST(PGP_SYM_DECRYPT(atp.""AveragePrice""::bytea, @Key) as double precision) as AverageTradedPrice,
                    CAST(PGP_SYM_DECRYPT(atp.""TotalBought""::bytea, @Key) as double precision) as TotalBought,
                    CAST(PGP_SYM_DECRYPT(atp.""Quantity""::bytea, @Key) as int) as Quantity,
                    @AccountId as AccountId
                  FROM ""AverageTradedPrices"" atp
                  WHERE atp.""AccountId"" = @AccountId
                ";

            if (!tickers.IsNullOrEmpty())
            {
                sql += @" AND PGP_SYM_DECRYPT(atp.""Ticker""::bytea, @Key) = ANY(@Tickers);";
            }

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<AverageTradedPriceDto>(sql, parameters);

            Auditor.Audit($"{nameof(AverageTradedPrice)}:{AuditOperation.View}", comment: "Preços médios de tickers do investidor foram descriptografados e visualizados.");
            return response;
        }
        #endregion

        #region DELETE
        public async Task RemoveByTickerNameAsync(Guid accountId, IEnumerable<string> tickers)
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);
            parameters.Add("@AccountId", accountId);
            parameters.Add("@Tickers", tickers);

            string sql =
                @"DELETE FROM ""AverageTradedPrices"" atp
                WHERE atp.""AccountId"" = @AccountId AND 
                PGP_SYM_DECRYPT(atp.""Ticker""::bytea, @Key) IN @Tickers";

            var connection = context.Database.GetDbConnection();
            
            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            Auditor.Audit($"{nameof(AverageTradedPrice)}:{AuditOperation.Delete}",
                comment: "Os preços médios de tickers do investidor foram removidos da base pois foram totalmente vendidos.",
                fields: new { RowsAffected = rowsAffected });
        }
        #endregion

        #region UPDATE
        public async Task UpdateAsync(Models.Account account, Models.AverageTradedPrice averageTradedPrice)
        {
            DynamicParameters parameters = new();

            string key = this.key.Value.Value;

            parameters.Add("@Key", key);
            parameters.Add("@Ticker", averageTradedPrice.Ticker);
            parameters.Add("@AveragePrice", averageTradedPrice.AveragePrice);
            parameters.Add("@TotalBought", averageTradedPrice.TotalBought);
            parameters.Add("@Quantity", averageTradedPrice.Quantity);
            parameters.Add("@AccountId", account.Id);

            string sql =
                @"UPDATE ""AverageTradedPrices"" SET 
                    ""AveragePrice"" = PGP_SYM_ENCRYPT(@AveragePrice, @Key),
                    ""TotalBought"" = PGP_SYM_ENCRYPT(@TotalBought, @Key),
	                ""Quantity"" = PGP_SYM_ENCRYPT(@Quantity, @Key)
                  WHERE ""AccountId"" = @AccountId AND
                  PGP_SYM_DECRYPT(""Ticker""::bytea, @Key) = @Ticker
                ";

            var connection = context.Database.GetDbConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            Auditor.Audit($"{nameof(AverageTradedPrice)}:{AuditOperation.Update}",
                comment: "Os preços médios de tickers do investidor foram atualizados na base.",
                fields: new { RowsAffected = rowsAffected });
        }
        #endregion
    }
}
