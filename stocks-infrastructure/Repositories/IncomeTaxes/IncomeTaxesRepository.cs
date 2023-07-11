using Dapper;
using Microsoft.EntityFrameworkCore;
using stocks.Database;
using stocks_infrastructure.Dtos;
using System.Text;

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

        public async Task<IEnumerable<SpecifiedMonthAssetsIncomeTaxesDto>> GetSpecifiedMonthAssetsIncomeTaxes(string month, Guid accountId)
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
	                it.""AssetId""
                FROM ""IncomeTaxes"" it
                WHERE it.""Month"" = @Month AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<SpecifiedMonthAssetsIncomeTaxesDto>(sql, parameters);

            return response;
        }
    }
}
