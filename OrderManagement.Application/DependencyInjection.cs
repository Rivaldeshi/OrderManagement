using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Services;

namespace OrderManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection));

        // Services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IOrderStatusService, OrderStatusService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}