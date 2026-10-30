using System.Text.Json.Serialization;

namespace VehicleExplorer.Api.Vpic;

// Raw vPIC response shapes. Every vPIC endpoint wraps its data as { Count, Message, SearchCriteria, Results }.

internal sealed record VpicResponse<T>(IReadOnlyList<T>? Results);

internal sealed record VpicMake(
    [property: JsonPropertyName("Make_ID")] int MakeId,
    [property: JsonPropertyName("Make_Name")] string? MakeName);

internal sealed record VpicVehicleType(
    [property: JsonPropertyName("VehicleTypeId")] int VehicleTypeId,
    [property: JsonPropertyName("VehicleTypeName")] string? VehicleTypeName);

internal sealed record VpicModel(
    [property: JsonPropertyName("Model_ID")] int ModelId,
    [property: JsonPropertyName("Model_Name")] string? ModelName);
