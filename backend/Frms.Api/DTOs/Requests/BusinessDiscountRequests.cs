using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

public sealed record CreateBusinessDiscountRequest(
    [property: Required, MaxLength(150)] string Name,
    [property: Range(typeof(decimal), "0", "100")] decimal Percentage,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo);
public sealed record UpdateBusinessDiscountRequest(
    [property: Required, MaxLength(150)] string Name,
    [property: Range(typeof(decimal), "0", "100")] decimal Percentage,
    [property: Required] string Status,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo);
