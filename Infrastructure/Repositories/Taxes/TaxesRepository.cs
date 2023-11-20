using Dapper;
using Microsoft.EntityFrameworkCore;
using Api.Database;
using Infrastructure.Dtos;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using System;

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
	                it.""Taxes"" as Taxes,
                    it.""Paid"",
	                it.""TradedAssets"",
	                it.""AssetId""
                FROM ""IncomeTaxes"" it
                INNER JOIN ""Assets"" a ON it.""AssetId"" = a.""Id""
                WHERE it.""Month"" = @Month AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();

            var response = await connection.QueryAsync<SpecifiedMonthTaxesDto>(sql, parameters);

            foreach (var item in response)
            {
                item.SerializedTradedAssets = JsonConvert.DeserializeObject<IEnumerable<SpecifiedMonthTaxesDtoDetails>>(item.TradedAssets)!;
            }

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
	                it.""Taxes"" as Taxes,
                    it.""Paid"",
	                it.""SwingTradeProfit"",
	                it.""DayTradeProfit"",
	                it.""AssetId""
                FROM ""IncomeTaxes"" it
                INNER JOIN ""Assets"" a ON it.""AssetId"" = a.""Id""
                WHERE RIGHT(it.""Month"", 4) LIKE @Year 
                AND it.""Taxes"" > 0
                AND it.""AccountId"" = @AccountId;
            ";

            var connection = context.Database.GetDbConnection();
            var response = await connection.QueryAsync<SpecifiedYearTaxesDto>(sql, parameters);

            return response;
        }

        public async Task<IEnumerable<TaxesLessThanMinimumRequiredDto>> GetTaxesLessThanMinimumRequired(Guid accountId, string date)
        {
            DynamicParameters parameters = new();

            parameters.Add("@Date", date);
            parameters.Add("@AccountId", accountId);

            string sql = @"
                SELECT * FROM
                    (SELECT
                        it.""Month"",
                        SUM(it.""Taxes"") AS ""Total""
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
