using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Infrastructure.Data
{
    // Design-time factory for EF Core tools. Uses a placeholder local connection string.
    public class ECommerceDbContextFactory : IDesignTimeDbContextFactory<ECommerceDbContext>
    {
        public ECommerceDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ECommerceDbContext>();
            // Placeholder connection string - update in API configuration during deployment
            var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True;"; optionsBuilder.UseSqlServer(connectionString);
            return new ECommerceDbContext(optionsBuilder.Options);
        }
    }
}
