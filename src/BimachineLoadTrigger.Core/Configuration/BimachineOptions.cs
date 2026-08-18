using System.ComponentModel.DataAnnotations;

namespace BimachineLoadTrigger.Core.Configuration;

public sealed record BimachineOptions
{
    public const string SectionName = "Bimachine";

    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public required string AppKey { get; init; }

    [Required]
    public required string LoadCode { get; init; }

    public int PollingIntervalSeconds { get; init; } = 5;

    public int PollingTimeoutMinutes { get; init; } = 60;
}
