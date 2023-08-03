using stocks_infrastructure.Dtos;

namespace stocks_infrastructure.Repositories.Taxes
{
    public interface ITaxesRepository
    {
        Task AddAllAsync(List<Models.IncomeTaxes> incomeTaxes);
        Task AddAsync(Models.IncomeTaxes incomeTaxes);
        Task<IEnumerable<SpecifiedMonthTaxesDto>> GetSpecifiedMonthTaxes(string month, Guid accountId);
        Task<IEnumerable<SpecifiedYearTaxesDto>> GetSpecifiedYearTaxes(string year, Guid accountId);
        Task SetMonthAsPaidOrUnpaid(string month, Guid accountId);
    }
}
