using Microsoft.AspNetCore.Diagnostics;
using VehicleExplorer.Application.Exceptions;

namespace VehicleExplorer.Api.Middleware;

/// <summary>
/// Turns application exceptions into ProblemDetails responses. Anything else becomes a normal 500.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IResult? result = exception switch
        {
            ValidationException validationException => Results.ValidationProblem(validationException.Errors),

            ExternalServiceException => Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Vehicle data is temporarily unavailable",
                detail: "The NHTSA vPIC service did not respond. Please try again in a moment."),

            _ => null,
        };

        if (result is null)
        {
            return false;
        }

        await result.ExecuteAsync(httpContext);
        return true;
    }
}
