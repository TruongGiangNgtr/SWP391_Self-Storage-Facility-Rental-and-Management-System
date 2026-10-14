namespace Frms.Business.Models.Commands;

public sealed record UpdateCustomerProfileCommand(
    bool UpdateFullName, string? FullName,
    bool UpdateAddress, string? Address,
    bool UpdateCccd, string? Cccd);
