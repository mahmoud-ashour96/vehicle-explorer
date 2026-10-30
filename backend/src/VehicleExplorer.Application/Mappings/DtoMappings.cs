using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Domain.Entities;

namespace VehicleExplorer.Application.Mappings;

public static class DtoMappings
{
    public static MakeDto ToDto(this Make make) => new() { Id = make.Id, Name = make.Name };

    public static VehicleTypeDto ToDto(this VehicleType vehicleType) => new() { Id = vehicleType.Id, Name = vehicleType.Name };

    public static VehicleModelDto ToDto(this VehicleModel model) => new() { Id = model.Id, Name = model.Name };
}
