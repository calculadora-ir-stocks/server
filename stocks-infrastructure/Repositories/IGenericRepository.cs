namespace stocks.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        IEnumerable<T> GetAll();
        Task<T> GetByIdAsync(Guid id);
        public T GetById(Guid id);
    }
}
