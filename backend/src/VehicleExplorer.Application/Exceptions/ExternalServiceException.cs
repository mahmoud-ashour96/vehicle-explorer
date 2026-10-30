namespace VehicleExplorer.Application.Exceptions;

/// <summary>
/// The external vehicle data source could not be reached or returned unusable data.
/// </summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
