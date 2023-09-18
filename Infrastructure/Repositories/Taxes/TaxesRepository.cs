using Dapper;
using Microsoft.EntityFrameworkCore;
using Api.Database;
using Infrastructure.Dtos;

namespace Infrastructure.Repositories.Taxes
{
    public class TaxesRepository : ITaxesRepository
    {
        private readonly StocksContext context;

        public TaxesRepository(StocksContext context)
        {
            this.context = context;
        }

        public async Task AddAllAsync(List<Models.IncomeTaxes> incomeTaxes)
        {
            context.AddRange(incomeTaxes);

            context.AttachRange(incomeTaxes.Select(x => x.Account));

            await context.SaveChangesAsync();
        }

        public async Task AddAsync(Models.IncomeTaxes incomeTaxes)
        {
            context.Add(incomeTaxes);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SpecifiedMonthTaxesDto>> GetSpecifiedMonthTaxes(string month, Guid accountId)
        {
            DynamicParameters parameters = new();

            parameters.Add("@Month", month);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT
                    it.""Month"",
	                it.""TotalTaxes"" as Taxes,
	                it.""TotalSold"",
	                it.""SwingTradeProfit"",
	                it.""DayTradeProfit"",
	                it.""TradedAssets"",
	                it.""AssetId"",
                    a.""Name"" as AssetName
                FROM ""IncomeTaxes"" it
                INNER JOIN ""Assets"" a ON it.""AssetId"" = a.""Id""
                WHERE it.""Month"" = @Month AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<SpecifiedMonthTaxesDto>(sql, parameters);

            return response;
        }

        public async Task<IEnumerable<SpecifiedYearTaxesDto>> GetSpecifiedYearTaxes(string year, Guid accountId)
        {
            DynamicParameters parameters = new();

            parameters.Add("@Year", year);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT
                    LEFT(it.""Month"", 2) as Month,
	                it.""TotalTaxes"" as Taxes,
	                it.""SwingTradeProfit"",
	                it.""DayTradeProfit""
                FROM ""IncomeTaxes"" it
                INNER JOIN ""Assets"" a ON it.""AssetId"" = a.""Id""
                WHERE RIGHT(it.""Month"", 4) LIKE @Year 
                AND it.""TotalTaxes"" > 0
                AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<SpecifiedYearTaxesDto>(sql, parameters);

            return response;
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
