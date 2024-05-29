using System.Text.Json.Serialization;

namespace NuGet.PackageLifeCycle;

public enum ListedVerb
{
    Unchanged,
    Unlist,
    Relist,
}

public class DeprecationRequest
{
    [JsonPropertyName("versions")]
    public required List<string> Versions { get; init; }

    [JsonPropertyName("isLegacy")]
    public bool? IsLegacy { get; set; }

    [JsonPropertyName("hasCriticalBugs")]
    public bool? HasCriticalBugs { get; set; }

    [JsonPropertyName("isOther")]
    public bool? IsOther { get; set; }

    [JsonPropertyName("alternatePackageId")]
    public string? AlternatePackageId { get; set; }

    [JsonPropertyName("alternatePackageVersion")]
    public string? AlternatePackageVersion { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("listedVerb")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ListedVerb ListedVerb { get; set; }
}
