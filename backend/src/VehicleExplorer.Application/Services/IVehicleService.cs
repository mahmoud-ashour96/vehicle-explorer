using VehicleExplorer.Application.Dtos;

namespace VehicleExplorer.Application.Services;

public interface IVehicleService
{
    Task<IReadOnlyList<MakeDto>> GetMakesAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<int> GetModelYears();

    Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleModelDto>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default);
}
