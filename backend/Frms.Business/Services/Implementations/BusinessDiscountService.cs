using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class BusinessDiscountService(IBusinessDiscountRepository repository,
    ICurrentUserContext user, IAuthenticationService auth) : IBusinessDiscountService
{
    public async Task<BusinessDiscountRecord> CreateAsync(
        Guid customerId, string name, decimal percentage, DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo, CancellationToken ct)
    {
        await RequireBOMAsync(ct);
        Validate(name, percentage, "ACTIVE", effectiveFrom, effectiveTo);
        if (customerId == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "CustomerId is required.", 400);
        return await repository.CreateAsync(customerId, name.Trim(), percentage,
            effectiveFrom.UtcDateTime, effectiveTo?.UtcDateTime, ct)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "Customer not found.", 404);
    }

    public async Task<BusinessDiscountRecord> UpdateAsync(
        Guid id, string name, decimal percentage, string status,
        DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, CancellationToken ct)
    {
        await RequireBOMAsync(ct);
        Validate(name, percentage, status, effectiveFrom, effectiveTo);
        try
        {
            return await repository.UpdateAsync(id, name.Trim(), percentage,
                status, effectiveFrom.UtcDateTime, effectiveTo?.UtcDateTime, ct)
                ?? throw new BusinessException("RESOURCE_NOT_FOUND", "Discount not found.", 404);
        }
        catch (DiscountMutationConflictException)
        {
            throw new BusinessException("DISCOUNT_IN_USE",
                "Referenced Discount financial fields cannot be changed.", 409);
        }
    }

    private async Task RequireBOMAsync(CancellationToken ct)
    {
        if (!user.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var current = await auth.GetCurrentAccountAsync(user.UserAccountId, ct);
        if (user.Role != "BUSINESS_OPERATIONS_MANAGER" ||
            current.Role != "BUSINESS_OPERATIONS_MANAGER")
            throw new BusinessException("FORBIDDEN", "Access denied.", 403);
    }

    private static void Validate(string name, decimal pct, string status,
        DateTimeOffset start, DateTimeOffset? end)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150 ||
            pct < 0 || pct > 100 || status is not ("ACTIVE" or "INACTIVE") ||
            end.HasValue && end.Value < start)
            throw new BusinessException("VALIDATION_ERROR", "Invalid Discount fields.", 400);
    }
}
