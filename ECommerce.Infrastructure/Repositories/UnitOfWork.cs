using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of IUnitOfWork wrapping EF Core DbContext transactions.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ECommerceDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ECommerceDbContext context)
        {
            _context = context;
        }

        public async Task<ITransaction> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
            return new Transaction(_transaction);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        /// <summary>
        /// Inner adapter for IDbContextTransaction to implement ITransaction.
        /// </summary>
        private class Transaction : ITransaction
        {
            private readonly IDbContextTransaction _dbTransaction;

            public Transaction(IDbContextTransaction dbTransaction)
            {
                _dbTransaction = dbTransaction;
            }

            public async Task CommitAsync()
            {
                await _dbTransaction.CommitAsync().ConfigureAwait(false);
            }

            public async Task RollbackAsync()
            {
                await _dbTransaction.RollbackAsync().ConfigureAwait(false);
            }

            public async ValueTask DisposeAsync()
            {
                await _dbTransaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
