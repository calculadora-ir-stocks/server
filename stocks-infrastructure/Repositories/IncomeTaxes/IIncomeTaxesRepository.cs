namespace stocks_infrastructure.Repositories.IncomeTaxes
{
    public interface IIncomeTaxesRepository
    {
        Task AddAsync(Models.IncomeTaxes incomeTaxes);
    }
}
