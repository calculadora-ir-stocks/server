namespace Infrastructure.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        Task<T> GetByIdAsync(Guid id);
    }
}
