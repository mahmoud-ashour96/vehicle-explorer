using System.Text.Json.Serialization;

namespace VehicleExplorer.Infrastructure.Vpic;

// vPIC response shapes. Every endpoint wraps its data as { Count, Message, SearchCriteria, Results }.

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
    public int VehicleTypeId { get; set; }

    public string? VehicleTypeName { get; set; }
}

internal class VpicModelResult
{
    [JsonPropertyName("Model_ID")]
    public int ModelId { get; set; }

    [JsonPropertyName("Model_Name")]
    public string? ModelName { get; set; }
}
