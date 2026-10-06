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

            services.AddHttpClient<IProductServiceClient, OrderService.Infrastructure.Clients.ProductServiceClient>()
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                    options.Retry.UseJitter = true;
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.MinimumThroughput = 5;
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                });
                
            services.AddHttpClient<IPaymentServiceClient, OrderService.Infrastructure.Clients.PaymentServiceClient>()
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                    options.Retry.UseJitter = true;
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.MinimumThroughput = 5;
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                });

            services.AddScoped<IOrderRepository, OrderService.Infrastructure.Repositories.OrderRepository>();
            services.AddScoped<ICartRepository, OrderService.Infrastructure.Repositories.CartRepository>();
            services.AddScoped<IIdempotencyRepository, OrderService.Infrastructure.Repositories.IdempotencyRepository>();
            services.AddScoped<IOutboxRepository, OrderService.Infrastructure.Repositories.OutboxRepository>();
            services.AddScoped<IUnitOfWork, OrderService.Infrastructure.Repositories.UnitOfWork>();

            // Application Services
            services.AddSingleton<OrderService.Application.Interfaces.Services.ISystemClock, OrderService.Infrastructure.Services.SystemClock>();
            services.AddScoped<OrderService.Application.Interfaces.Services.ICartService, OrderService.Application.Services.CartService>();
            services.AddScoped<OrderService.Application.Interfaces.Services.IOrderService, OrderService.Application.Services.OrderServiceImpl>();
            services.AddScoped<OrderService.Application.Interfaces.Services.IOutboxService, OrderService.Application.Services.OutboxService>();

            return services;
        }
    }
}
