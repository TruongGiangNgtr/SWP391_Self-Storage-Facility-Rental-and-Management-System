namespace Frms.Business.Models.Commands;

public sealed record CustomerLoginCommand(string PhoneNumber, string Password, string? IpAddress, string? DeviceInfo);
public sealed record EmployeeLoginCommand(string Email, string Password, string? IpAddress, string? DeviceInfo);
