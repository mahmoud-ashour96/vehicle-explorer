using Microsoft.AspNetCore.Diagnostics;
using VehicleExplorer.Api.Vpic;

namespace VehicleExplorer.Api.Infrastructure;

/// <summary>Turns <see cref="UpstreamServiceException"/> into a 502 ProblemDetails response.</summary>
public sealed class UpstreamExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not UpstreamServiceException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Vehicle data is temporarily unavailable",
                Detail = "The NHTSA vPIC service did not respond. Please try again in a moment.",
            },
        });
    }
}
