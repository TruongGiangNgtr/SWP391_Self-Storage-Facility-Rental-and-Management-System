namespace Frms.Business.Models;

public sealed record DiscountResult(
    Guid DiscountId,
    Guid CustomerId,
    string Name,
    decimal Percentage,
    string Status,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);
