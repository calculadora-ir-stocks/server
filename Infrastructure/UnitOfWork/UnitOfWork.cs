using Api.Database;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbConnection connection;
        private readonly DbTransaction transaction = null!;

        public UnitOfWork(StocksContext stocksContext)
        {
            connection = stocksContext.Database.GetDbConnection();
        }

        public async Task<DbTransaction> BeginTransactionAsync()
        {
            await connection.OpenAsync();
            return await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        }

        #region IDisposable

        // To detect redundant calls.
        private bool _disposed;

        // Public implementation of Dispose pattern callable by consumers.
        ~UnitOfWork() => Dispose(false);

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Dispose managed state (managed objects).
            if (disposing)
            {
                connection?.Dispose();
                transaction?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
