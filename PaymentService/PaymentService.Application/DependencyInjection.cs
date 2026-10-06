using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Interfaces.Services;

namespace PaymentService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IPaymentService, Services.PaymentService>();
            return services;
        }
    }
}
