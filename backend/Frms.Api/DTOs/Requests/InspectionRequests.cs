using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for recording inspection damage (INS-004).</summary>
public sealed record RecordDamageRequest
{
    public required Guid DamageTypeId { get; init; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal DamageAmount { get; init; }

    public string? Note { get; init; }
}

/// <summary>Payload for recording an inspection extra fee (INS-005).</summary>
public sealed record RecordExtraFeeRequest
{
    public required Guid ExtraFeeTypeId { get; init; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal Amount { get; init; }

    [Required]
    public required string Reason { get; init; }
}

/// <summary>Multipart payload for inspection evidence (INS-006).</summary>
public sealed record UploadInspectionEvidenceRequest
{
    [Required]
    public required IFormFile File { get; init; }

    [Required]
    public required string EvidenceType { get; init; }
}

/// <summary>Payload for completing an inspection (INS-007).</summary>
public sealed record CompleteInspectionRequest
{
    [Required]
    public required string ConditionNote { get; init; }
}

/// <summary>Payload for a Facility Manager damage decision (INS-009).</summary>
public sealed record DecideDamageRequest
{
    [Required]
    public required string Decision { get; init; }
}
