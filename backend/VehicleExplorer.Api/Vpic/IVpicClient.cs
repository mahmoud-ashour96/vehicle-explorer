using VehicleExplorer.Api.Models;

namespace VehicleExplorer.Api.Vpic;

/// <summary>Access to the NHTSA vPIC API. Implementations throw <see cref="UpstreamServiceException"/> on any upstream failure.</summary>
public interface IVpicClient
{
    Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken ct);

    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken ct);

    Task<IReadOnlyList<VehicleModel>> GetModelsAsync(int makeId, int year, string? vehicleType, CancellationToken ct);
}
