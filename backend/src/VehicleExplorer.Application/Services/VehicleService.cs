using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Application.Mappings;
using VehicleExplorer.Domain.Rules;

namespace VehicleExplorer.Application.Services;

public class VehicleService : IVehicleService
{
    // vPIC data rarely changes and the full makes list takes a few seconds to download, so results are cached.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    private readonly IVehicleDataSource _dataSource;
    private readonly IMemoryCache _cache;

    public VehicleService(IVehicleDataSource dataSource, IMemoryCache cache)
    {
        _dataSource = dataSource;
        _cache = cache;
    }

    public async Task<IReadOnlyList<MakeDto>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        var makes = await _cache.GetOrCreateAsync("makes", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return _dataSource.GetMakesAsync(cancellationToken);
        });

        return makes!.OrderBy(m => m.Name).Select(m => m.ToDto()).ToList();
    }

    public IReadOnlyList<int> GetModelYears()
    {
        return ModelYears.All();
    }

    public async Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default)
    {
        var vehicleTypes = await _cache.GetOrCreateAsync($"vehicle-types:{makeId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return _dataSource.GetVehicleTypesAsync(makeId, cancellationToken);
        });

        return vehicleTypes!.OrderBy(t => t.Name).Select(t => t.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<VehicleModelDto>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default)
    {
        if (!ModelYears.IsValid(year))
        {
            throw new ValidationException(nameof(year), $"Model year must be between {ModelYears.Min} and {ModelYears.Max}.");
        }

        var models = await _cache.GetOrCreateAsync($"models:{makeId}:{year}:{vehicleType}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return _dataSource.GetModelsAsync(makeId, year, vehicleType, cancellationToken);
        });

        return models!.OrderBy(m => m.Name).Select(m => m.ToDto()).ToList();
    }
}
