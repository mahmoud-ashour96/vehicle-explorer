using Microsoft.Extensions.DependencyInjection;
using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache(options => options.SizeLimit = 5_000);
        services.AddScoped<IVehicleService, VehicleService>();

        return services;
    }
}
