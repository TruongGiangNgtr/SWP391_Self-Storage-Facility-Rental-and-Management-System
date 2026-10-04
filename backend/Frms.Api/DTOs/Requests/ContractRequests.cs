using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for renewing a contract (CON-003).</summary>
public sealed record RenewContractRequest
{
    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public required string NewEndMonth { get; init; }
}
