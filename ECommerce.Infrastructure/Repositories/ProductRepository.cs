using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ECommerceDbContext context) : base(context)
        {
        }

        public override async Task<Product?> GetByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
        {
            return await _dbSet.Include(p => p.Inventory).FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Product>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return await _dbSet.Include(p => p.Inventory).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Product?> GetBySkuAsync(string sku)
        {
            return await _dbSet.Include(p => p.Inventory).AsNoTracking().FirstOrDefaultAsync(p => p.SKU == sku).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId)
        {
            return await _dbSet.Include(p => p.Inventory).Where(p => p.CategoryId == categoryId).AsNoTracking().ToListAsync().ConfigureAwait(false);
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection, ProductStatus? status = null)
        {
            var query = _dbSet.Include(p => p.Inventory).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.SKU.Contains(term));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var total = await query.CountAsync().ConfigureAwait(false);

            // Apply sorting
            var ordering = (sortBy ?? "name").Trim();
            var direction = (sortDirection ?? "asc").Trim().ToLowerInvariant();

            IOrderedQueryable<Product> orderedQuery;
            switch (ordering.ToLowerInvariant())
            {
                case "name":
                    orderedQuery = direction == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);
                    break;
                case "price":
                    orderedQuery = direction == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price);
                    break;
                case "createdat":
                case "created_at":
                    orderedQuery = direction == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                    break;
                default:
                    // fallback to name
                    orderedQuery = query.OrderBy(p => p.Name);
                    break;
            }

            var items = await orderedQuery
                .AsNoTracking()
                .Skip((Math.Max(1, pageNumber) - 1) * Math.Max(1, pageSize))
                .Take(Math.Max(1, pageSize))
                .ToListAsync()
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            return await _dbSet.Include(p => p.Inventory).Where(p => p.Status == ProductStatus.Active).AsNoTracking().ToListAsync().ConfigureAwait(false);
        }
    }
}
