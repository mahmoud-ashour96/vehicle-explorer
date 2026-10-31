using System.Net.Http.Json;
using System.Text.Json;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Application.Interfaces;
using VehicleExplorer.Domain.Entities;

namespace VehicleExplorer.Infrastructure.Vpic;

/// <summary>
/// Reads vehicle data from the NHTSA vPIC API and maps it to domain entities.
/// </summary>
public class VpicVehicleDataSource : IVehicleDataSource
{
    private readonly HttpClient _httpClient;

    public VpicVehicleDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        var results = await GetResultsAsync<VpicMakeResult>("getallmakes?format=json", cancellationToken);

        // Some vPIC make names have leading or trailing spaces.
        return results
            .Select(r => new Make { Id = r.MakeId, Name = r.MakeName?.Trim() ?? string.Empty })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default)
    {
        var results = await GetResultsAsync<VpicVehicleTypeResult>(
            $"GetVehicleTypesForMakeId/{makeId}?format=json", cancellationToken);

        return results
            .Select(r => new VehicleType { Id = r.VehicleTypeId, Name = r.VehicleTypeName ?? string.Empty })
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

        // vPIC can return the same model more than once.
        return results
            .DistinctBy(r => r.ModelId)
            .Select(r => new VehicleModel { Id = r.ModelId, Name = r.ModelName ?? string.Empty })
            .ToList();
    }

    private async Task<List<T>> GetResultsAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<VpicResponse<T>>(url, cancellationToken);
            return response?.Results ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // HttpRequestException: network error or error status code.
            // JsonException: vPIC sometimes answers with an HTML error page.
            throw new ExternalServiceException("The NHTSA vPIC service is unavailable.", ex);
        }
    }
}
