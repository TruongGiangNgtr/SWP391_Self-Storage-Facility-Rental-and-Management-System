using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for the optional AI unit-type recommendation endpoint (AI-001).</summary>
public sealed record RecommendUnitTypeRequest
{
    [Required]
    public required string StorageDescription { get; init; }

    [Required]
    public required string PreferredMode { get; init; }
}
