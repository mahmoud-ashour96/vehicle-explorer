using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Infrastructure.Vpic;

namespace VehicleExplorer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VpicOptions>()
            .Bind(configuration.GetSection(VpicOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IVehicleDataSource, VpicVehicleDataSource>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<VpicOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddStandardResilienceHandler(); // retries with backoff, timeouts and a circuit breaker

        return services;
    }
}
