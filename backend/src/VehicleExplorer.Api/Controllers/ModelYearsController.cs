using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Application.Services;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/model-years")]
public class ModelYearsController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public ModelYearsController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // GET api/model-years
    [HttpGet]
    public ActionResult<IReadOnlyList<int>> GetModelYears()
    {
        return Ok(_vehicleService.GetModelYears());
    }
}
