using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

public sealed record CreateBusinessDiscountRequest(
    [Required, MaxLength(150)] string Name,
    [Range(typeof(decimal), "0", "100")] decimal Percentage,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo);
public sealed record UpdateBusinessDiscountRequest(
    [Required, MaxLength(150)] string Name,
    [Range(typeof(decimal), "0", "100")] decimal Percentage,
    [Required] string Status,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo);
