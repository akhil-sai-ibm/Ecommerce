using System;
using System.Threading.Tasks;

namespace ECommerce.Application.Interfaces.Repositories
{
    /// <summary>
    /// Abstraction for managing database transactions.
    /// Allows Application layer to use transactions without coupling to EF Core.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <returns>A transaction that must be disposed or committed/rolled back.</returns>
        Task<ITransaction> BeginTransactionAsync();

        /// <summary>
        /// Commits all pending changes to the database.
        /// </summary>
        /// <returns>Number of entities written to the database.</returns>
        Task<int> SaveChangesAsync();
    }

    /// <summary>
    /// Represents a database transaction.
    /// </summary>
    public interface ITransaction : IAsyncDisposable
    {
        /// <summary>
        /// Commits the transaction.
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        Task RollbackAsync();
    }
}
