using Api.Database;
using Common.Constants;
using Common;
using Dapper;
using Infrastructure.Dtos;
using Infrastructure.Models;
using Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Common.Configurations;

namespace Infrastructure.Repositories.Taxes
{
    public class IncomeTaxesRepository : IIncomeTaxesRepository
    {
        private readonly StocksContext context;
        private readonly IUnitOfWork unitOfWork;
        private readonly AzureKeyVaultConfiguration keyVault;

        public IncomeTaxesRepository(StocksContext context, IUnitOfWork unitOfWork, AzureKeyVaultConfiguration keyVault)
        {
            this.context = context;
            this.unitOfWork = unitOfWork;
            this.keyVault = keyVault;
        }

        public async Task AddAsync(IncomeTaxes incomeTaxes)
        {
            DynamicParameters parameters = new();

            var key = await keyVault.SecretClient.GetSecretAsync("pgcrypto-key");

            parameters.Add("@Key", key.Value.Value);
            parameters.Add("@AssetId", incomeTaxes.AssetId);
            parameters.Add("@Month", incomeTaxes.Month);
            parameters.Add("@Taxes", incomeTaxes.Taxes);
            parameters.Add("@TotalSold", incomeTaxes.TotalSold);
            parameters.Add("@Paid", incomeTaxes.Paid);
            parameters.Add("@SwingTradeProfit", incomeTaxes.SwingTradeProfit);
            parameters.Add("@DayTradeProfit", incomeTaxes.DayTradeProfit);
            parameters.Add("@SwingTradeProfit", incomeTaxes.SwingTradeProfit);
            parameters.Add("@TradedAssets", incomeTaxes.TradedAssets);
            parameters.Add("@CompesatedSwingTradeLoss", incomeTaxes.CompesatedSwingTradeLoss);
            parameters.Add("@CompesatedDayTradeLoss", incomeTaxes.CompesatedDayTradeLoss);
            parameters.Add("@AccountId", incomeTaxes.Account.Id);

            string sql = @"
                INSERT INTO ""IncomeTaxes""
                (
	                ""Id"",
	                ""AssetId"",
	                ""Month"",
	                ""Taxes"",
	                ""TotalSold"",
	                ""Paid"",
	                ""SwingTradeProfit"",
	                ""DayTradeProfit"",
	                ""TradedAssets"",
	                ""CompesatedSwingTradeLoss"",
	                ""CompesatedDayTradeLoss"",
	                ""AccountId""
                )
                VALUES 
                (
	                gen_random_uuid(),
                    @AssetId,
                    @Month,
                    PGP_SYM_ENCRYPT(@Taxes, @Key),
                    PGP_SYM_ENCRYPT(@TotalSold, @Key),
                    @Paid,
                    PGP_SYM_ENCRYPT(@SwingTradeProfit, @Key),
                    PGP_SYM_ENCRYPT(@DayTradeProfit, @Key),
                    PGP_SYM_ENCRYPT(@TradedAssets, @Key),
                    @CompesatedSwingTradeLoss,
                    @CompesatedDayTradeLoss,
                    @AccountId
                )
            ";

            await context.Database.GetDbConnection().QueryAsync(sql, parameters);
            Auditor.Audit($"{nameof(IncomeTaxes)}:{AuditOperation.Add}", comment: "Todas as informações de imposto do usuário foram criptografadas na base de dados.");
        }

        public async Task<IEnumerable<SpecifiedMonthTaxesDto>> GetSpecifiedMonthTaxes(string month, Guid accountId)
        {
            DynamicParameters parameters = new();

            var key = await keyVault.SecretClient.GetSecretAsync("pgcrypto-key");

            parameters.Add("@Key", key.Value.Value);
            parameters.Add("@Month", month);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT
                    it.""Month"",
	                CAST(PGP_SYM_DECRYPT(it.""Taxes""::bytea, @Key) as double precision) AS Taxes,
                    it.""Paid"",
	                PGP_SYM_DECRYPT(it.""TradedAssets""::bytea, @Key) AS TradedAssets,
	                it.""AssetId""
                FROM ""IncomeTaxes"" it
                INNER JOIN ""Assets"" a ON it.""AssetId"" = a.""Id""
                WHERE it.""Month"" = @Month AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<SpecifiedMonthTaxesDto>(sql, parameters);

            foreach (var item in response)
                item.SerializedTradedAssets = JsonConvert.DeserializeObject<IEnumerable<SpecifiedMonthTaxesDtoDetails>>(item.TradedAssets)!;

            return response;
        }

        public async Task<IEnumerable<SpecifiedYearTaxesDto>> GetSpecifiedYearTaxes(string year, Guid accountId)
        {
            DynamicParameters parameters = new();

            var key = await keyVault.SecretClient.GetSecretAsync("pgcrypto-key");

            parameters.Add("@Key", key.Value.Value);
            parameters.Add("@Year", year);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT
                    LEFT(it.""Month"", 2) as Month,
	                CAST(PGP_SYM_DECRYPT(it.""Taxes""::bytea, @Key) as double precision) AS Taxes,
                    it.""Paid"",
	                CAST(PGP_SYM_DECRYPT(it.""SwingTradeProfit""::bytea, @Key) as double precision) AS SwingTradeProfit,
	                CAST(PGP_SYM_DECRYPT(it.""DayTradeProfit""::bytea, @Key) as double precision) AS DayTradeProfit,
	                it.""AssetId""
                FROM ""IncomeTaxes"" it
                INNER JOIN ""Assets"" a ON it.""AssetId"" = a.""Id""
                WHERE RIGHT(it.""Month"", 4) LIKE @Year 
                AND CAST(PGP_SYM_DECRYPT(it.""Taxes""::bytea, @Key) as double precision) > 0
                AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<SpecifiedYearTaxesDto>(sql, parameters);

            return response;
        }

        public async Task<IEnumerable<TaxesLessThanMinimumRequiredDto>> GetTaxesLessThanMinimumRequired(Guid accountId, string date)
        {
            DynamicParameters parameters = new();

            var key = await keyVault.SecretClient.GetSecretAsync("pgcrypto-key");

            parameters.Add("@Key", key.Value.Value);
            parameters.Add("@Date", date);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT * FROM
                    (SELECT
                        it.""Month"",
                        SUM(CAST(PGP_SYM_DECRYPT(it.""Taxes""::bytea, @Key) as double precision)) AS ""Total""
                    FROM ""IncomeTaxes"" it
                    WHERE it.""AccountId"" = @AccountId
                    AND it.""Paid"" IS FALSE
                    AND to_date(it.""Month"", 'MM/YYYY') < to_date(@Date, 'MM/YYYY')
                    GROUP BY it.""Month"") AS it
                WHERE it.""Total"" < 10;
            ";

            var connection = context.Database.GetDbConnection();
            var taxes = await connection.QueryAsync<TaxesLessThanMinimumRequiredDto>(sql, parameters);

            if (taxes.IsNullOrEmpty()) return Enumerable.Empty<TaxesLessThanMinimumRequiredDto>();

            return taxes;
        }

        public async Task SetMonthAsPaidOrUnpaid(string month, Guid accountId)
        {
            DynamicParameters parameters = new();

            parameters.Add("@Month", month);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                UPDATE ""IncomeTaxes"" i
                SET ""Paid"" =
                    (CASE
                        WHEN i.""Paid"" = TRUE THEN FALSE
                        ELSE TRUE
                    END)
                WHERE i.""Month"" = @Month AND i.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            await connection.QueryAsync(sql, parameters);
        }
    }
}