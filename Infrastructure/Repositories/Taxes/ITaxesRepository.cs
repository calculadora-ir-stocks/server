using Infrastructure.Dtos;

namespace Infrastructure.Repositories.Taxes
{
    public interface ITaxesRepository
    {
        Task AddAsync(Models.IncomeTaxes incomeTaxes);
        Task<IEnumerable<SpecifiedMonthTaxesDto>> GetSpecifiedMonthTaxes(string month, Guid accountId);
        Task<IEnumerable<SpecifiedYearTaxesDto>> GetSpecifiedYearTaxes(string year, Guid accountId);

        /// <summary>
        /// Retorna todos os meses anteriores ao mês atual em que há impostos menores que o valor mínimo de R$10,00.
        /// </summary>
        /// <param name="accountId">O id da conta</param>
        /// <param name="date">O mês atual no formato MM/yyyy</param>
        /// <returns></returns>
        Task<IEnumerable<TaxesLessThanMinimumRequiredDto>> GetTaxesLessThanMinimumRequired(Guid accountId, string date);
        Task SetMonthAsPaidOrUnpaid(string month, Guid accountId);
    }
}
