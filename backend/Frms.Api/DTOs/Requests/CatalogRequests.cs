using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>AI-001, stateless public input. Limit guards provider cost and anonymous abuse.</summary>
public sealed record RecommendUnitTypeRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string StorageDescription { get; init; }

    [Required]
    [RegularExpression("^(PUBLIC|PRIVATE)$", ErrorMessage = "PreferredMode must be PUBLIC or PRIVATE.")]
    public required string PreferredMode { get; init; }
}
