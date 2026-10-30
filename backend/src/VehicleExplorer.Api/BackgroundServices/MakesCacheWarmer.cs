using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Api.BackgroundServices;

/// <summary>
/// Loads the makes list once at startup so the first visitor doesn't wait for the slow vPIC call.
/// If vPIC is down we only log it; the list is then fetched on the first request instead.
/// </summary>
public class MakesCacheWarmer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MakesCacheWarmer> _logger;

    public MakesCacheWarmer(IServiceScopeFactory scopeFactory, ILogger<MakesCacheWarmer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

            var makes = await vehicleService.GetMakesAsync(stoppingToken);
            _logger.LogInformation("Makes cache warmed with {Count} makes", makes.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not warm the makes cache at startup");
        }
    }
}
