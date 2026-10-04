using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for creating a Facility (BOM-002).</summary>
public sealed record CreateFacilityRequest
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Address { get; init; }

    public string? ContactInfo { get; init; }

    public string? Description { get; init; }
}

/// <summary>Payload for updating the current UnitType rental price (BOM-007).</summary>
public sealed record UpdateUnitTypePriceRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal RentalPrice { get; init; }
}

/// <summary>Payload for creating an immutable Policy version (BOM-009).</summary>
public sealed record CreatePolicyVersionRequest
{
    [Range(1, int.MaxValue)]
    public required int DepositTimeoutHours { get; init; }

    [Range(1, 31)]
    public required int ReservationVisitStartDay { get; init; }

    [Range(1, 31)]
    public required int ReservationVisitEndDay { get; init; }

    [Range(1, 31)]
    public required int MonthlyPaymentDueDay { get; init; }

    [Range(1, 31)]
    public required int OverdueStartDay { get; init; }

    [Range(1, int.MaxValue)]
    public required int LateFeeDivisorDays { get; init; }

    [Range(1, 31)]
    public required int EarlyReturnWaiveFeeUntilDay { get; init; }
}

/// <summary>Payload for creating a customer-owned Discount (BOM-011).</summary>
public sealed record CreateCustomerDiscountRequest
{
    [Required]
    public required string Name { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public required decimal Percentage { get; init; }

    public required DateTimeOffset EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveTo { get; init; }
}

/// <summary>Payload for updating the allowed Discount master fields (BOM-012).</summary>
public sealed record UpdateDiscountRequest
{
    [Required]
    public required string Name { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public required decimal Percentage { get; init; }

    [Required]
    public required string Status { get; init; }

    public required DateTimeOffset EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveTo { get; init; }
}

/// <summary>Payload for updating an ExtraFeeType (BOM-014).</summary>
public sealed record UpdateExtraFeeTypeRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal DefaultAmount { get; init; }

    [Required]
    public required string Status { get; init; }
}
