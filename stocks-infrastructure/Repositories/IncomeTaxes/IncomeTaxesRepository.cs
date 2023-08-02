using Dapper;
using Microsoft.EntityFrameworkCore;
using stocks.Database;
using stocks_infrastructure.Dtos;

namespace stocks_infrastructure.Repositories.IncomeTaxes
{
    public class IncomeTaxesRepository : IIncomeTaxesRepository
    {
        private readonly StocksContext context;

        public IncomeTaxesRepository(StocksContext context)
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

        public async Task<IEnumerable<SpecifiedMonthTaxesDto>> GetSpecifiedMonthAssetsIncomeTaxes(string month, Guid accountId)
        {
            DynamicParameters parameters = new();

            parameters.Add("@Month", month);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT
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

        public async Task<IEnumerable<SpecifiedYearTaxesDto>> GetSpecifiedYearAssetsIncomeTaxes(string year, Guid accountId)
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
    }
}
