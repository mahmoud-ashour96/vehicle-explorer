using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VehicleExplorer.Api.Models;
using VehicleExplorer.Api.Services;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public sealed class VehiclesController(VehicleCatalog catalog, TimeProvider clock) : ControllerBase
{
    private const int OneHour = 3600;

    /// <summary>All makes (~12k), sorted by name. The client filters this list locally for autocomplete.</summary>
    [HttpGet("makes")]
    [ResponseCache(Duration = OneHour)]
    public Task<IReadOnlyList<Make>> GetMakes(CancellationToken ct) => catalog.GetMakesAsync(ct);

    /// <summary>The model years the API accepts, newest first.</summary>
    [HttpGet("model-years")]
    public IReadOnlyList<int> GetModelYears() => ModelYears.All(clock.GetUtcNow());

    [HttpGet("makes/{makeId:int:min(1)}/vehicle-types")]
    [ResponseCache(Duration = OneHour)]
    public Task<IReadOnlyList<VehicleType>> GetVehicleTypes(int makeId, CancellationToken ct) =>
        catalog.GetVehicleTypesAsync(makeId, ct);

    /// <param name="makeId">vPIC make id.</param>
    /// <param name="year">Model year.</param>
    /// <param name="vehicleType">Optional vehicle type name as returned by the vehicle-types endpoint.</param>
    /// <param name="ct">Request cancellation.</param>
    [HttpGet("makes/{makeId:int:min(1)}/models")]
    [ResponseCache(Duration = OneHour)]
    [ProducesResponseType<IReadOnlyList<VehicleModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<VehicleModel>>> GetModels(
        int makeId,
        [FromQuery, BindRequired] int year,
        // Allow-list rather than passing raw input into the upstream URL path.
        [FromQuery, StringLength(100), RegularExpression(@"^[\w\s()\-]+$",
            ErrorMessage = "Vehicle type may only contain letters, digits, spaces, parentheses and hyphens.")]
        string? vehicleType,
        CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (!ModelYears.IsValid(year, now))
        {
            ModelState.AddModelError(nameof(year), $"Model year must be between {ModelYears.Min} and {ModelYears.Max(now)}.");
            return ValidationProblem(ModelState);
        }

        return Ok(await catalog.GetModelsAsync(makeId, year, vehicleType, ct));
    }
}
