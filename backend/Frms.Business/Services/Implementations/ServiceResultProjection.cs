using Frms.Business.Models.Results;
using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Services.Implementations;

internal static class ServiceResultProjection
{
    internal static AccountProfileResult Map(UserAccount row) => new(row.UserAccountId, row.Email, row.PhoneNumber, row.Status, row.CreatedAt);
    internal static CustomerProfileResult? Map(Customer? row) => row is null ? null : new(row.CustomerId, row.FullName, row.Address, row.Cccd);
    internal static EmployeeProfileResult Map(Employee row) => new(row.EmployeeId, row.FacilityId, row.FullName);
    internal static FacilityResult Map(Facility row) => new(row.FacilityId, row.Name, row.Address, row.ContactInfo, row.Description, row.Status);
    internal static UnitTypeResult Map(UnitType row) => new(row.UnitTypeId, row.Name, row.Mode, row.Size, row.RentalPrice, row.Description);
    internal static VisitResult Map(Visit row) => new(row.VisitId, row.EntityId, row.EmployeeId, row.VisitType, row.VisitDate, row.ActualReturnDate, row.Status);
    internal static (AccountProfileResult Account, EmployeeProfileResult Employee, string RoleName) Map((UserAccount Account, Employee Employee, string RoleName) row) => (Map(row.Account), Map(row.Employee), row.RoleName);
    internal static (AccountProfileResult Account, EmployeeProfileResult Employee, string RoleName)? Map((UserAccount Account, Employee Employee, string RoleName)? row) => row is null ? null : Map(row.Value);
    internal static (AccountProfileResult Account, string RoleName, CustomerProfileResult? Customer, EmployeeProfileResult? Employee) Map((UserAccount Account, string RoleName, Customer? Customer, Employee? Employee) row) => (Map(row.Account), row.RoleName, Map(row.Customer), row.Employee is null ? null : Map(row.Employee));
    internal static (AccountProfileResult Account, string RoleName, CustomerProfileResult? Customer, EmployeeProfileResult? Employee)? Map((UserAccount Account, string RoleName, Customer? Customer, Employee? Employee)? row) => row is null ? null : Map(row.Value);
}
