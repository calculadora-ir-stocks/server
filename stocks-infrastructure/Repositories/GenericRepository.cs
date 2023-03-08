using stocks.Database;

namespace stocks.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {

        private readonly StocksContext _context;

        public GenericRepository(StocksContext context)
        {
            _context = context;
        }

#pragma warning disable CS8603 // Possible null reference return.
        public void Add(T entity)
        {
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
            _context.SaveChanges();
        }

        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
            _context.SaveChanges();
        }

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>().AsEnumerable();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public T GetById(Guid id)
        {
            return _context.Set<T>().Find(id);
        }
    }
}
