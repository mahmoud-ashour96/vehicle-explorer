using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Infrastructure.Vpic;

namespace VehicleExplorer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var vpicBaseUrl = configuration["Vpic:BaseUrl"]
            ?? throw new InvalidOperationException("Missing setting 'Vpic:BaseUrl' in appsettings.json.");

        services.AddHttpClient<IVehicleDataSource, VpicVehicleDataSource>(client =>
            client.BaseAddress = new Uri(vpicBaseUrl));

        return services;
    }
}
