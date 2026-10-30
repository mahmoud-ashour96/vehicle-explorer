namespace VehicleExplorer.Api.Vpic;

/// <summary>vPIC could not be reached or returned something we could not use. Mapped to HTTP 502.</summary>
public sealed class UpstreamServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
