using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Interfaces.Services;
using ProductService.Application.Services;

namespace ProductService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IProductService, Services.ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IInventoryService, InventoryService>();
            return services;
        }
    }
}
