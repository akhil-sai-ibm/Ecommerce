using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces.Repositories;
using OrderService.Application.Interfaces.Clients;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<OrderServiceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(OrderServiceDbContext).Assembly.FullName)));

            services.AddHttpClient<IProductServiceClient, OrderService.Infrastructure.Clients.ProductServiceClient>();

            services.AddScoped<IOrderRepository, OrderService.Infrastructure.Repositories.OrderRepository>();
            services.AddScoped<ICartRepository, OrderService.Infrastructure.Repositories.CartRepository>();
            services.AddScoped<IPaymentRepository, OrderService.Infrastructure.Repositories.PaymentRepository>();
            services.AddScoped<IIdempotencyRepository, OrderService.Infrastructure.Repositories.IdempotencyRepository>();
            services.AddScoped<IOutboxRepository, OrderService.Infrastructure.Repositories.OutboxRepository>();
            services.AddScoped<IUnitOfWork, OrderService.Infrastructure.Repositories.UnitOfWork>();

            // Application Services
            services.AddScoped<OrderService.Application.Interfaces.Services.ICartService, OrderService.Application.Services.CartService>();
            services.AddScoped<OrderService.Application.Interfaces.Services.IOrderService, OrderService.Application.Services.OrderServiceImpl>();
            services.AddScoped<OrderService.Application.Interfaces.Services.IOutboxService, OrderService.Application.Services.OutboxService>();

            return services;
        }
    }
}
