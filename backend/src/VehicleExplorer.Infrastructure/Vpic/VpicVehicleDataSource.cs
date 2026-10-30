using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Domain.Entities;

namespace VehicleExplorer.Infrastructure.Vpic;

/// <summary>
/// Reads vehicle data from the NHTSA vPIC API and maps it to domain entities.
/// Retries, timeouts and the circuit breaker are configured on the HttpClient in DependencyInjection.cs.
/// </summary>
public class VpicVehicleDataSource : IVehicleDataSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VpicVehicleDataSource> _logger;

    public VpicVehicleDataSource(HttpClient httpClient, ILogger<VpicVehicleDataSource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        var results = await GetResultsAsync<VpicMakeResult>("getallmakes?format=json", cancellationToken);

        return results
            .Where(r => !string.IsNullOrWhiteSpace(r.MakeName))
            .DistinctBy(r => r.MakeId)
            .Select(r => new Make { Id = r.MakeId, Name = r.MakeName!.Trim() })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default)
    {
        var results = await GetResultsAsync<VpicVehicleTypeResult>(
            $"GetVehicleTypesForMakeId/{makeId}?format=json", cancellationToken);

        return results
            .Where(r => !string.IsNullOrWhiteSpace(r.VehicleTypeName))
            .DistinctBy(r => r.VehicleTypeId)
            .Select(r => new VehicleType { Id = r.VehicleTypeId, Name = r.VehicleTypeName!.Trim() })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken cancellationToken = default)
    {
        var url = $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}";
        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            url += $"/vehicletype/{Uri.EscapeDataString(vehicleType)}";
        }

        var results = await GetResultsAsync<VpicModelResult>($"{url}?format=json", cancellationToken);

        return results
            .Where(r => !string.IsNullOrWhiteSpace(r.ModelName))
            .DistinctBy(r => r.ModelId)
            .Select(r => new VehicleModel { Id = r.ModelId, Name = r.ModelName!.Trim() })
            .ToList();
    }

    private async Task<List<T>> GetResultsAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<VpicResponse<T>>(relativeUrl, cancellationToken);
            return response?.Results ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or ExecutionRejectedException)
        {
            // HttpRequestException: network error or non-success status code.
            // JsonException: vPIC sometimes answers with an HTML error page.
            // ExecutionRejectedException: Polly timeout or open circuit breaker.
            _logger.LogWarning(ex, "Request to vPIC failed: {Url}", relativeUrl);
            throw new ExternalServiceException("The NHTSA vPIC service is unavailable.", ex);
        }
    }
}
