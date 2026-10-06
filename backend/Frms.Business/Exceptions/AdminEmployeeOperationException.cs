namespace Frms.Business.Exceptions;

public sealed class AdminEmployeeOperationException(
    string code,
    string message,
    string? field = null) : Exception(message) {
    public string Code { get; } = code;

    public string? Field { get; } = field;
}
