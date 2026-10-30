using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VehicleExplorer.Application.Dtos;
using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/makes")]
[Produces("application/json")]
public class MakesController : ControllerBase
{
    private const int OneHourInSeconds = 3600;

    private readonly IVehicleService _vehicleService;

    public MakesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <summary>
    /// Gets all makes, sorted by name.
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = OneHourInSeconds)]
    [ProducesResponseType<IReadOnlyList<MakeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MakeDto>>> GetMakes(CancellationToken cancellationToken)
    {
        var makes = await _vehicleService.GetMakesAsync(cancellationToken);
        return Ok(makes);
    }

    /// <summary>
    /// Gets the vehicle types for a make.
    /// </summary>
    [HttpGet("{makeId:int}/vehicle-types")]
    [ResponseCache(Duration = OneHourInSeconds)]
    [ProducesResponseType<IReadOnlyList<VehicleTypeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> GetVehicleTypes(
        int makeId, CancellationToken cancellationToken)
    {
        var vehicleTypes = await _vehicleService.GetVehicleTypesAsync(makeId, cancellationToken);
        return Ok(vehicleTypes);
    }

    /// <summary>
    /// Gets the models for a make and model year, optionally filtered by vehicle type.
    /// </summary>
    /// <param name="makeId">The make id.</param>
    /// <param name="year">The model year, e.g. 2015.</param>
    /// <param name="vehicleType">Optional vehicle type name, e.g. "Passenger Car".</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpGet("{makeId:int}/models")]
    [ResponseCache(Duration = OneHourInSeconds)]
    [ProducesResponseType<IReadOnlyList<VehicleModelDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<VehicleModelDto>>> GetModels(
        int makeId,
        [FromQuery, BindRequired] int year,
        [FromQuery] string? vehicleType,
        CancellationToken cancellationToken)
    {
        var models = await _vehicleService.GetModelsAsync(makeId, year, vehicleType, cancellationToken);
        return Ok(models);
    }
}
