namespace VehicleExplorer.Application.Exceptions;

/// <summary>
/// The external vehicle data source failed or returned unusable data. Returned as 502.
/// </summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
