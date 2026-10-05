using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for creating a physical storage unit (UNIT-002).</summary>
public sealed record CreateStorageUnitRequest
{
    public required Guid UnitTypeId { get; init; }

    [Required]
    public required string UnitCode { get; init; }

    public string? LocationInfo { get; init; }
}

/// <summary>Payload for updating allowed storage-unit fields (UNIT-004).</summary>
public sealed record UpdateStorageUnitRequest
{
    public required Guid UnitTypeId { get; init; }

    public string? LocationInfo { get; init; }
}

/// <summary>Payload for an operational storage-unit status transition (UNIT-005).</summary>
public sealed record ChangeStorageUnitStatusRequest
{
    [Required]
    public required string Status { get; init; }
}
