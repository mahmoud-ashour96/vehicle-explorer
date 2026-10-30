using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Application.Mappings;
using VehicleExplorer.Domain.Rules;

namespace VehicleExplorer.Application.Services;

public class VehicleService : IVehicleService
{
    // Vehicle data rarely changes, and the full makes list is slow to fetch, so results are cached.
    private static readonly TimeSpan MakesCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan VehicleTypesCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ModelsCacheDuration = TimeSpan.FromHours(6);

    // The vehicle type ends up in the data source's URL path, so only allow plain names like "Passenger Car".
    private static readonly Regex VehicleTypePattern = new(@"^[\w\s()\-]{1,100}$", RegexOptions.Compiled);

    private readonly IVehicleDataSource _dataSource;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    public VehicleService(IVehicleDataSource dataSource, IMemoryCache cache, TimeProvider timeProvider)
    {
        _dataSource = dataSource;
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<MakeDto>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        return GetFromCacheOrLoadAsync("makes", MakesCacheDuration, async () =>
        {
            var makes = await _dataSource.GetMakesAsync(cancellationToken);
            return makes
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .Select(m => m.ToDto())
                .ToList();
        });
    }

    public IReadOnlyList<int> GetModelYears()
    {
        return ModelYears.All(_timeProvider.GetUtcNow());
    }

    public Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default)
    {
        ValidateMakeId(makeId);

        return GetFromCacheOrLoadAsync($"vehicle-types:{makeId}", VehicleTypesCacheDuration, async () =>
        {
            var vehicleTypes = await _dataSource.GetVehicleTypesAsync(makeId, cancellationToken);
            return vehicleTypes
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => t.ToDto())
                .ToList();
        });
    }

    public Task<IReadOnlyList<VehicleModelDto>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default)
    {
        ValidateMakeId(makeId);

        var now = _timeProvider.GetUtcNow();
        if (!ModelYears.IsValid(year, now))
        {
            throw new ValidationException(
                nameof(year), $"Model year must be between {ModelYears.Min} and {ModelYears.Max(now)}.");
        }

        vehicleType = string.IsNullOrWhiteSpace(vehicleType) ? null : vehicleType.Trim();
        if (vehicleType is not null && !VehicleTypePattern.IsMatch(vehicleType))
        {
            throw new ValidationException(
                nameof(vehicleType), "Vehicle type may only contain letters, digits, spaces, parentheses and hyphens.");
        }

        var cacheKey = $"models:{makeId}:{year}:{vehicleType?.ToLowerInvariant()}";
        return GetFromCacheOrLoadAsync(cacheKey, ModelsCacheDuration, async () =>
        {
            var models = await _dataSource.GetModelsAsync(makeId, year, vehicleType, cancellationToken);
            return models
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .Select(m => m.ToDto())
                .ToList();
        });
    }

    private static void ValidateMakeId(int makeId)
    {
        if (makeId <= 0)
        {
            throw new ValidationException(nameof(makeId), "Make id must be a positive number.");
        }
    }

    private async Task<IReadOnlyList<T>> GetFromCacheOrLoadAsync<T>(
        string cacheKey, TimeSpan duration, Func<Task<List<T>>> load)
    {
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached) && cached is not null)
        {
            return cached;
        }

        // Failures throw before this point, so errors are never cached.
        var result = await load();

        // Size = 1 per entry; the SizeLimit configured in Program.cs caps how many entries are kept.
        _cache.Set<IReadOnlyList<T>>(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration,
            Size = 1,
        });

        return result;
    }
}
