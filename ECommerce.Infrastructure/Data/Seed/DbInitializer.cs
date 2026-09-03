using System;
using System.Collections.Generic;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Data.Seed
{
    public static class DbInitializer
    {
        public static IEnumerable<Role> GetRoles()
        {
            return new[]
            {
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Customer", CreatedAt = DateTime.UtcNow },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Admin", CreatedAt = DateTime.UtcNow }
            };
        }

        public static IEnumerable<Category> GetCategories()
        {
            return new[]
            {
                new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Electronics", Description = "Electronic devices and accessories", CreatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Books", Description = "Books and literature", CreatedAt = DateTime.UtcNow }
            };
        }

        public static IEnumerable<Product> GetProducts()
        {
            return new[]
            {
                new Product { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Smartphone", Description = "A modern smartphone", Price = 699.99m, SKU = "SPH-001", Status = Domain.Enums.ProductStatus.Active, CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333333"), CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Novel", Description = "A fiction novel", Price = 19.99m, SKU = "BOOK-001", Status = Domain.Enums.ProductStatus.Active, CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444"), CreatedAt = DateTime.UtcNow }
            };
        }

        public static IEnumerable<Inventory> GetInventories()
        {
            return new[]
            {
                new Inventory { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), ProductId = Guid.Parse("55555555-5555-5555-5555-555555555555"), Quantity = 100, ReorderLevel = 10, CreatedAt = DateTime.UtcNow },
                new Inventory { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), ProductId = Guid.Parse("66666666-6666-6666-6666-666666666666"), Quantity = 50, ReorderLevel = 5, CreatedAt = DateTime.UtcNow }
            };
        }
    }
}
