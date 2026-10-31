using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/makes")]
public class MakesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public MakesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // GET api/makes
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MakeDto>>> GetMakes(CancellationToken cancellationToken)
    {
        var makes = await _vehicleService.GetMakesAsync(cancellationToken);
        return Ok(makes);
    }

    // GET api/makes/474/vehicle-types
    [HttpGet("{makeId:int}/vehicle-types")]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> GetVehicleTypes(
        int makeId, CancellationToken cancellationToken)
    {
        var vehicleTypes = await _vehicleService.GetVehicleTypesAsync(makeId, cancellationToken);
        return Ok(vehicleTypes);
    }

    // GET api/makes/474/models?year=2015&vehicleType=Passenger%20Car
    [HttpGet("{makeId:int}/models")]
    public async Task<ActionResult<IReadOnlyList<VehicleModelDto>>> GetModels(
        int makeId, [FromQuery] int year, [FromQuery] string? vehicleType, CancellationToken cancellationToken)
    {
        var models = await _vehicleService.GetModelsAsync(makeId, year, vehicleType, cancellationToken);
        return Ok(models);
    }
}
