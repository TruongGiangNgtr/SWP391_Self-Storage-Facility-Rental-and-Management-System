using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Api.Mapping;

internal static class CustomerMapping
{
    public static UpdatedCustomerProfileResponse ToResponse(this UpdatedCustomerProfileResult x) =>
        new(x.UserAccountId, x.CustomerId, x.Role, x.Status, x.FullName);

    public static CustomerDirectoryResponse ToResponse(this CustomerDirectoryRecord x) =>
        new(x.CustomerId, x.FullName);

    public static PaginatedResponse<CustomerDirectoryResponse> ToResponse(this CustomerDirectoryPageResult x) =>
        new(x.Items.Select(y => y.ToResponse()).ToArray(),
            new PaginationResponse(x.Page, x.PageSize, x.TotalItems, x.TotalPages));
}
