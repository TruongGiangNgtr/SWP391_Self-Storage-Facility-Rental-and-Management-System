namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the authenticated account summary returned with a token.</summary>
public sealed record AuthenticatedUserResponse(
    Guid UserAccountId,
    string Role,
    string Status);

/// <summary>Represents the canonical authentication-token response (AUTH-002/AUTH-003).</summary>
public sealed record AuthTokenResponse(
    AuthTokenDataResponse Data,
    string Message);

/// <summary>Represents the data carried by an authentication-token response.</summary>
public sealed record AuthTokenDataResponse(
    string AccessToken,
    string TokenType,
    AuthenticatedUserResponse User);
