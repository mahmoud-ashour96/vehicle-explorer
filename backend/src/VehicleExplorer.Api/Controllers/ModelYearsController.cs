using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/model-years")]
[Produces("application/json")]
public class ModelYearsController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public ModelYearsController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <summary>
    /// Gets the supported model years, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<int>> GetModelYears()
    {
        return Ok(_vehicleService.GetModelYears());
    }
}
