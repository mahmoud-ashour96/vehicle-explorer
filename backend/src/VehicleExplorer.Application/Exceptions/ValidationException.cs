namespace VehicleExplorer.Application.Exceptions;

/// <summary>
/// The request breaks a business rule, for example an unsupported model year. Returned as 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]> { [propertyName] = [errorMessage] };
    }

    public Dictionary<string, string[]> Errors { get; }
}
