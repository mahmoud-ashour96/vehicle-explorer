namespace VehicleExplorer.Api.Services;

/// <summary>
/// Loads the makes list once at startup so the first visitor doesn't wait for the slow getallmakes call.
/// Best effort: if vPIC is down, we log it and the normal request path fetches on demand.
/// </summary>
public sealed class MakesCacheWarmer(IServiceScopeFactory scopeFactory, ILogger<MakesCacheWarmer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var catalog = scope.ServiceProvider.GetRequiredService<VehicleCatalog>();

            var makes = await catalog.GetMakesAsync(stoppingToken);
            logger.LogInformation("Makes cache warmed with {Count} makes", makes.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not warm the makes cache at startup");
        }
    }
}
