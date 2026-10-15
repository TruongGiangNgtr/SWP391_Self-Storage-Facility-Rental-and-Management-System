namespace Frms.Api.DTOs.Responses;

/// <summary>AUTH-001 registration returns identifiers only, never credentials.</summary>
public sealed record RegisterCustomerResponse(Guid UserAccountId, Guid CustomerId);

/// <summary>Represents the canonical authentication-token response (AUTH-002/AUTH-003).</summary>
public sealed record AuthTokenResponse(
    AuthTokenDataResponse Data,
    string Message);
