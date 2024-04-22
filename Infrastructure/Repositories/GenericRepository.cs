using Api.Database;

namespace Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {

        private readonly StocksContext _context;

        public GenericRepository(StocksContext context)
        {
            _context = context;
        }

#pragma warning disable CS8603 // Possible null reference return.

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>().AsEnumerable();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            return await _context.Set<T>().FindAsync(id);
        }
    }
}
