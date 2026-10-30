using System.Text.Json;
using Polly;
using VehicleExplorer.Api.Models;

namespace VehicleExplorer.Api.Vpic;

/// <summary>
/// Typed HttpClient for vPIC. Retries, timeouts and the circuit breaker come from the resilience
/// handler registered in Program.cs; this class only builds URLs and maps responses.
/// </summary>
public sealed class VpicClient(HttpClient http, ILogger<VpicClient> logger) : IVpicClient
{
    public async Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken ct)
    {
        var results = await GetResultsAsync<VpicMake>("getallmakes?format=json", ct);

        return results
            .Where(m => !string.IsNullOrWhiteSpace(m.MakeName))
            .Select(m => new Make(m.MakeId, m.MakeName!.Trim()))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken ct)
    {
        var results = await GetResultsAsync<VpicVehicleType>($"GetVehicleTypesForMakeId/{makeId}?format=json", ct);

        return results
            .Where(t => !string.IsNullOrWhiteSpace(t.VehicleTypeName))
            .DistinctBy(t => t.VehicleTypeId)
            .Select(t => new VehicleType(t.VehicleTypeId, t.VehicleTypeName!.Trim()))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId, int year, string? vehicleType, CancellationToken ct)
    {
        var path = $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}";
        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            path += $"/vehicletype/{Uri.EscapeDataString(vehicleType.Trim())}";
        }

        var results = await GetResultsAsync<VpicModel>($"{path}?format=json", ct);

        return results
            .Where(m => !string.IsNullOrWhiteSpace(m.ModelName))
            .DistinctBy(m => m.ModelId)
            .Select(m => new VehicleModel(m.ModelId, m.ModelName!.Trim()))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<T>> GetResultsAsync<T>(string relativeUrl, CancellationToken ct)
    {
        try
        {
            var response = await http.GetFromJsonAsync<VpicResponse<T>>(relativeUrl, ct);
            return response?.Results ?? [];
        }
        // HttpRequestException: network error or non-2xx status. JsonException: vPIC sometimes answers
        // with an HTML error page. ExecutionRejectedException: Polly timeout or open circuit.
        catch (Exception ex) when (ex is HttpRequestException or JsonException or ExecutionRejectedException)
        {
            logger.LogWarning(ex, "vPIC request {Url} failed", relativeUrl);
            throw new UpstreamServiceException("The NHTSA vPIC service is unavailable.", ex);
        }
    }
}
