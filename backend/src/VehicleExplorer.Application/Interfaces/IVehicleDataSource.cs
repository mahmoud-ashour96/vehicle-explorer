using VehicleExplorer.Domain.Entities;

namespace VehicleExplorer.Application.Interfaces;

/// <summary>
/// Where vehicle data comes from. Implemented in Infrastructure by the NHTSA vPIC client.
/// </summary>
public interface IVehicleDataSource
{
    Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default);
}
