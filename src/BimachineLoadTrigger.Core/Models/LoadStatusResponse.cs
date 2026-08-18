using System.Text.Json.Serialization;

namespace BimachineLoadTrigger.Core.Models;

public sealed record LoadStatusResponse(
    [property: JsonPropertyName("id")] long? Id,
    [property: JsonPropertyName("loadType")] string? LoadType,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("startDate")] long? StartDate,
    [property: JsonPropertyName("endDate")] long? EndDate,
    [property: JsonPropertyName("log")] string? Log)
{
    [JsonIgnore]
    public bool IsFinished => EndDate is not null;

    [JsonIgnore]
    public bool IsError => string.Equals(Status, "ERROR", StringComparison.OrdinalIgnoreCase);
}
