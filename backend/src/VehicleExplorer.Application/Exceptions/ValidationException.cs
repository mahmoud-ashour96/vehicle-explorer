namespace VehicleExplorer.Application.Exceptions;

/// <summary>
/// The request breaks a business rule (for example an unsupported model year).
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]> { [propertyName] = [errorMessage] };
    }

    public IDictionary<string, string[]> Errors { get; }
}
