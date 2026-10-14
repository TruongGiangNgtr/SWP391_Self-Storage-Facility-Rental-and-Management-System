using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class BusinessDiscountRepository(FrmsDbContext db) : IBusinessDiscountRepository
{
    public async Task<BusinessDiscountRecord?> CreateAsync(
        Guid customerId, string name, decimal percent, DateTime fromUtc,
        DateTime? toUtc, CancellationToken ct)
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.CustomerId == customerId, ct))
            return null;
        var entity = new Discount
        {
            DiscountId = Guid.NewGuid(), CustomerId = customerId, Name = name,
            Percentage = percent, Status = "ACTIVE", EffectiveFrom = fromUtc, EffectiveTo = toUtc
        };
        db.Discounts.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<BusinessDiscountRecord?> UpdateAsync(
        Guid discountId, string name, decimal percent, string status,
        DateTime fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        // Serialize referenced-discount guard and update to avoid a concurrent
        // new Contract observing an unsafe changed historical Discount snapshot.
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var entity = await db.Discounts.SingleOrDefaultAsync(x => x.DiscountId == discountId, ct);
        if (entity is null) return null;
        var referenced = await db.Contracts.AsNoTracking()
            .AnyAsync(x => x.DiscountId == discountId, ct);
        if (referenced && (entity.Percentage != percent || entity.EffectiveFrom != fromUtc ||
                           entity.EffectiveTo != toUtc))
            throw new DiscountMutationConflictException();
        entity.Name = name;
        entity.Percentage = percent;
        entity.Status = status;
        entity.EffectiveFrom = fromUtc;
        entity.EffectiveTo = toUtc;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ToRecord(entity);
    }

    private static BusinessDiscountRecord ToRecord(Discount x) => new(
        x.DiscountId, x.CustomerId, x.Name, x.Percentage, x.Status,
        x.EffectiveFrom, x.EffectiveTo);
}
