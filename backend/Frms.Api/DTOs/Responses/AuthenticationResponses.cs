namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the canonical authentication-token response (AUTH-002/AUTH-003).</summary>
public sealed record AuthTokenResponse(
    AuthTokenDataResponse Data,
    string Message);
