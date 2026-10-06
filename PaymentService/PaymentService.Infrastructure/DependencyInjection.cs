using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Interfaces.Clients;
using PaymentService.Application.Interfaces.Providers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Infrastructure.Clients;
using PaymentService.Infrastructure.Data;
using PaymentService.Infrastructure.Providers;
using PaymentService.Infrastructure.Repositories;
using System;
using System.Net.Http;

namespace PaymentService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PaymentServiceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IPaymentProvider, MockPaymentProvider>();

            services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
            {
                var url = configuration["ServiceUrls:OrderService"] ?? "http://localhost:5032";
                client.BaseAddress = new Uri(url);
            })
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

            return services;
        }
    }
}
