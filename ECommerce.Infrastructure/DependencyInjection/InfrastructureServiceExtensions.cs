using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Authentication;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Notifications;

namespace ECommerce.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext using SQL Server connection string from configuration
            // Development-only diagnostic: log the DataSource and Database name (do NOT log full connection strings or secrets)
            // Register DbContext using SQL Server connection string from configuration
            services.AddDbContext<ECommerceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Unit of Work for transaction management
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Generic repository
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Specialized repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IInventoryRepository, InventoryRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

            // Authentication services
            services.AddScoped<IPasswordHasher, AspNetCorePasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();

            // Caching
            services.AddMemoryCache();
            services.AddScoped<ICacheService, CacheService>();

            // Notifications
            services.AddScoped<IOrderNotificationService, OrderNotificationService>();

            return services;
        }
    }
}
