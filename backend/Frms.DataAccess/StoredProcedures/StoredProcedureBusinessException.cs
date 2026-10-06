namespace Frms.DataAccess.StoredProcedures;

public sealed class StoredProcedureBusinessException(
    string code,
    string message)
    : Exception(message)
{
    public string Code { get; } = code;
}