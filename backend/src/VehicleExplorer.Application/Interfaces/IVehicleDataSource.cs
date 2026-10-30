using VehicleExplorer.Domain.Entities;

namespace VehicleExplorer.Application.Interfaces;

/// <summary>
/// Where vehicle data comes from. Implemented in Infrastructure (currently the NHTSA vPIC API).
/// Implementations throw <see cref="Exceptions.ExternalServiceException"/> when the source is unavailable.
/// </summary>
public interface IVehicleDataSource
{
    Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default);
}
