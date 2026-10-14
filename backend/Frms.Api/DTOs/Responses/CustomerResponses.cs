namespace Frms.Api.DTOs.Responses;

// Mirrors the frontend AuthUser fields used after AUTH-005; login identifiers stay unchanged.
public sealed record UpdatedCustomerProfileResponse(
    Guid UserAccountId, Guid CustomerId, string Role, string Status, string FullName);

// Deliberately excludes CCCD, Address, PhoneNumber, Email and authentication secrets.
// Only this minimum projection is safe while the proposed field-level PII contract is open.
public sealed record CustomerDirectoryResponse(Guid CustomerId, string FullName);
