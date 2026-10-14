namespace Frms.Api.DTOs.Responses;

public sealed record AdminCustomerStatusResponse(Guid CustomerId, Guid UserAccountId, string Status);
