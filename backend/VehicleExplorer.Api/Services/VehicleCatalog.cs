using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Api.Models;
using VehicleExplorer.Api.Vpic;

namespace VehicleExplorer.Api.Services;

/// <summary>
/// Caches vPIC lookups. The data changes rarely, and getallmakes alone is ~600 KB and takes seconds,
/// so serving it from memory is the biggest latency win in the app. Failures are not cached.
/// </summary>
public sealed class VehicleCatalog(IVpicClient vpic, IMemoryCache cache)
{
    private static readonly TimeSpan MakesTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan VehicleTypesTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan ModelsTtl = TimeSpan.FromHours(6);

    public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken ct) =>
        GetOrFetchAsync("makes", MakesTtl, () => vpic.GetMakesAsync(ct));

    public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken ct) =>
        GetOrFetchAsync($"vehicle-types:{makeId}", VehicleTypesTtl, () => vpic.GetVehicleTypesAsync(makeId, ct));

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken ct)
    {
        var type = string.IsNullOrWhiteSpace(vehicleType) ? null : vehicleType.Trim();
        var key = $"models:{makeId}:{year}:{type?.ToLowerInvariant()}";

        return GetOrFetchAsync(key, ModelsTtl, () => vpic.GetModelsAsync(makeId, year, type, ct));
    }

    private async Task<T> GetOrFetchAsync<T>(string key, TimeSpan ttl, Func<Task<T>> fetch)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var value = await fetch();

        // Size = 1 per entry; the cache's SizeLimit (Program.cs) caps how many lookups we keep.
        cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl, Size = 1 });
        return value;
    }
}
