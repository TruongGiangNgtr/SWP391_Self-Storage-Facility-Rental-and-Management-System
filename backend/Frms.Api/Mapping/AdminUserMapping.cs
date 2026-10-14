using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class AdminUserMapping
{
    public static AdminUserAccountResponse ToResponse(this (
        AccountProfileResult Account,
        string RoleName,
        CustomerProfileResult? Customer,
        EmployeeProfileResult? Employee) row)
    {
        AdminUserProfileResponse? profile = null;
        if (row.Customer is not null)
        {
            profile = new AdminUserProfileResponse(
                CustomerId: row.Customer.CustomerId,
                EmployeeId: null,
                FullName: row.Customer.FullName,
                Address: row.Customer.Address,
                Cccd: row.Customer.Cccd,
                FacilityId: null);
        }
        else if (row.Employee is not null)
        {
            profile = new AdminUserProfileResponse(
                CustomerId: null,
                EmployeeId: row.Employee.EmployeeId,
                FullName: row.Employee.FullName,
                Address: null,
                Cccd: null,
                FacilityId: row.Employee.FacilityId);
        }

        return new AdminUserAccountResponse(
            row.Account.UserAccountId, row.RoleName, row.Account.Status,
            row.Account.Email, row.Account.PhoneNumber, row.Account.CreatedAt, profile);
    }
}
