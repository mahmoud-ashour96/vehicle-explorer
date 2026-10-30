using System.ComponentModel.DataAnnotations;

namespace VehicleExplorer.Infrastructure.Vpic;

public class VpicOptions
{
    public const string SectionName = "Vpic";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
