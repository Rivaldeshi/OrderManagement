using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Vérifier si on est en environnement de test
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isTestEnvironment = environment == "Testing";

        // N'ajouter SQL Server QUE si on n'est PAS en test
        if (!isTestEnvironment)
        {
            services.AddDbContext<OrderManagementDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        }

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}