using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for creating an ACCESS visit (VIS-001).</summary>
public sealed record CreateAccessVisitRequest
{
    public required DateOnly VisitDate { get; init; }
}

/// <summary>Payload for creating a RETURN visit (VIS-002).</summary>
public sealed record CreateReturnVisitRequest
{
    public required DateOnly VisitDate { get; init; }
}

/// <summary>Payload for rescheduling a visit (VIS-005).</summary>
public sealed record RescheduleVisitRequest
{
    public required DateOnly VisitDate { get; init; }
}

/// <summary>Payload for cancelling a visit (VIS-006).</summary>
public sealed record CancelVisitRequest
{
    [Required]
    public required string Reason { get; init; }
}

/// <summary>Payload for completing a handover (OPS-004).</summary>
public sealed record CompleteHandoverRequest
{
    public required Guid VisitId { get; init; }

    public required Guid StorageUnitId { get; init; }

    public Guid? DiscountId { get; init; }
}

/// <summary>Payload for confirming an actual return date (OPS-005).</summary>
public sealed record ConfirmActualReturnRequest
{
    public required DateOnly ActualReturnDate { get; init; }
}
