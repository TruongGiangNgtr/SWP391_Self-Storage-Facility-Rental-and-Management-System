using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Models.Results;

public sealed record UpdatedCustomerProfileResult(
    Guid UserAccountId, Guid CustomerId, string Role, string Status, string FullName);

public sealed record CustomerDirectoryPageResult(
    IReadOnlyList<CustomerDirectoryRecord> Items,
    int Page, int PageSize, int TotalItems, int TotalPages);
