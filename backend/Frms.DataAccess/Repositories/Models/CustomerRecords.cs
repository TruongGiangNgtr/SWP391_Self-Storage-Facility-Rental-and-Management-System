namespace Frms.DataAccess.Repositories.Models;

public sealed record CustomerProfileRecord(Guid CustomerId, Guid UserAccountId, string FullName);

// Restrict cross-role business projections to non-sensitive fields.
public sealed record CustomerDirectoryRecord(Guid CustomerId, string FullName);

public sealed class CustomerCccdConflictException : Exception { }
