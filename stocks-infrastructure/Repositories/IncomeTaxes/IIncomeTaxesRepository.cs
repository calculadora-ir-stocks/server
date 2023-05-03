namespace stocks_infrastructure.Repositories.IncomeTaxes
{
    public interface IIncomeTaxesRepository
    {
        Task AddAllAsync(List<Models.IncomeTaxes> incomeTaxes);
        Task AddAsync(Models.IncomeTaxes incomeTaxes);
    }
}
