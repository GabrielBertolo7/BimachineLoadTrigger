using System.Text.Json.Serialization;

namespace BimachineLoadTrigger.Core.Models;

public sealed record ExecuteLoadResponse(
    [property: JsonPropertyName("id")] long Id);
