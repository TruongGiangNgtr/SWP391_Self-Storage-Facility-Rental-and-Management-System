namespace Frms.Business.Models.Results;

// Service outputs are immutable snapshots, never tracked persistence entities.
public sealed record AccountProfileResult(Guid UserAccountId, string Email, string PhoneNumber, string Status, DateTime CreatedAt);
public sealed record CustomerProfileResult(Guid CustomerId, string FullName, string? Address, string? Cccd);
public sealed record EmployeeProfileResult(Guid EmployeeId, Guid? FacilityId, string FullName);
public sealed record FacilityResult(Guid FacilityId, string Name, string Address, string? ContactInfo, string? Description, string Status);
public sealed record UnitTypeResult(Guid UnitTypeId, string Name, string Mode, string Size, decimal RentalPrice, string? Description);
public sealed record VisitResult(Guid VisitId, Guid EntityId, Guid? EmployeeId, string VisitType, DateOnly VisitDate, DateTime? ActualReturnDate, string Status);
public sealed record SupportTicketResult(Guid SupportTicketId, Guid ContractId, Guid CustomerId, Guid? AssignedEmployeeId, string Category, string Description, string Status, string? ResultNote, DateTime CreatedAt, DateTime? CompletedAt);
