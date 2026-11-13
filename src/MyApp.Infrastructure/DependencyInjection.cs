using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Abstractions;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        return services;
    }
}
