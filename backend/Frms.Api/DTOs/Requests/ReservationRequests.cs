using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for creating a reservation (RES-001).</summary>
public sealed record CreateReservationRequest
{
    public required Guid FacilityId { get; init; }

    public required Guid UnitTypeId { get; init; }

    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public required string StartMonth { get; init; }

    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public required string EndMonth { get; init; }
}

/// <summary>Payload for confirming a reservation (RES-004).</summary>
public sealed record ConfirmReservationRequest
{
    public required DateOnly ReservationVisitDate { get; init; }
}

/// <summary>Payload for cancelling a reservation (RES-005).</summary>
public sealed record CancelReservationRequest
{
    [Required]
    public required string Reason { get; init; }
}
