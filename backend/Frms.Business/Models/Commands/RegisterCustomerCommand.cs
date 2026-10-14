namespace Frms.Business.Models.Commands;

public sealed record RegisterCustomerCommand(
    string FullName,
    string PhoneNumber,
    string Email,
    string Password,
    string? Address,
    string? Cccd);
