using Microsoft.Extensions.DependencyInjection;
using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IVehicleService, VehicleService>();

        return services;
    }
}
