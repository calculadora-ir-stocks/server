using System.Data.Common;

namespace Infrastructure.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task<DbTransaction> BeginTransactionAsync();
    }
}
