using System.Text.Json.Serialization;

namespace VehicleExplorer.Infrastructure.Vpic;

// Raw vPIC response shapes. Every vPIC endpoint wraps its data as { Count, Message, SearchCriteria, Results }.

internal class VpicResponse<T>
{
    public List<T> Results { get; set; } = [];
}

internal class VpicMakeResult
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; set; }

    [JsonPropertyName("Make_Name")]
    public string? MakeName { get; set; }
}

internal class VpicVehicleTypeResult
{
    [JsonPropertyName("VehicleTypeId")]
    public int VehicleTypeId { get; set; }

    [JsonPropertyName("VehicleTypeName")]
    public string? VehicleTypeName { get; set; }
}

internal class VpicModelResult
{
    [JsonPropertyName("Model_ID")]
    public int ModelId { get; set; }

    [JsonPropertyName("Model_Name")]
    public string? ModelName { get; set; }
}
