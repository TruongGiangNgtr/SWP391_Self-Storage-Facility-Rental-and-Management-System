namespace Frms.DataAccess.Repositories.Models;

public sealed record BusinessDiscountRecord(Guid DiscountId, Guid CustomerId,
    string Name, decimal Percentage, string Status, DateTime EffectiveFrom, DateTime? EffectiveTo);
public sealed class DiscountMutationConflictException : Exception { }
