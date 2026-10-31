using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Domain.Entities;

namespace VehicleExplorer.Tests.Fakes;

/// <summary>
/// In-memory stand-in for the vPIC data source. Records calls so tests can check caching and arguments.
/// </summary>
public class FakeVehicleDataSource : IVehicleDataSource
{
    public List<Make> Makes { get; set; } =
    [
        new Make { Id = 448, Name = "TOYOTA" },
        new Make { Id = 474, Name = "HONDA" },
    ];

    public List<VehicleType> VehicleTypes { get; set; } =
    [
        new VehicleType { Id = 3, Name = "Truck" },
        new VehicleType { Id = 2, Name = "Passenger Car" },
    ];

    public List<VehicleModel> Models { get; set; } =
    [
        new VehicleModel { Id = 1863, Name = "Civic" },
        new VehicleModel { Id = 1861, Name = "Accord" },
        new VehicleModel { Id = 2128, Name = "CR-Z" }, // sorts after "Civic" only when sorting ignores case
    ];

    public bool ShouldFail { get; set; }

    public int GetMakesCallCount { get; private set; }

    public (int MakeId, int Year, string? VehicleType)? LastModelsRequest { get; private set; }

    public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        GetMakesCallCount++;
        return Respond(Makes);
    }

    public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default)
    {
        return Respond(VehicleTypes);
    }

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default)
    {
        LastModelsRequest = (makeId, year, vehicleType);
        return Respond(Models);
    }

    private Task<IReadOnlyList<T>> Respond<T>(List<T> items)
    {
        if (ShouldFail)
        {
            throw new ExternalServiceException("vPIC is down");
        }

        return Task.FromResult<IReadOnlyList<T>>(items);
    }
}
