using stocks_core.Responses;
using stocks_infrastructure.Dtos;

namespace stocks_infrastructure.Repositories.IncomeTaxes
{
    public interface IIncomeTaxesRepository
    {
        Task AddAllAsync(List<Models.IncomeTaxes> incomeTaxes);
        Task AddAsync(Models.IncomeTaxes incomeTaxes);
        Task<IEnumerable<SpecifiedMonthAssetsIncomeTaxesDto>> GetSpecifiedMonthAssetsIncomeTaxes(string month, Guid accountId);
    }
}
