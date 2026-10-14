namespace Frms.DataAccess.Repositories.Models;

public sealed record CustomerAccountStatusRecord(Guid CustomerId, Guid UserAccountId, string Status);
public sealed class CustomerStatusProcedureException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
