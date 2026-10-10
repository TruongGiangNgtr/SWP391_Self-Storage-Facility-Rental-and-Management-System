using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

public sealed record FinalizeReturnRequest
{
    [Required]
    public required string StorageUnitStatus { get; init; }
}
