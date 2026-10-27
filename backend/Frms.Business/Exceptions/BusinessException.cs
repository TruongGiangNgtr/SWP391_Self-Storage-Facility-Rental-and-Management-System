namespace Frms.Business.Exceptions;

public sealed class BusinessException(string code, string safeMessage, int suggestedStatusCode,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(safeMessage)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int SuggestedStatusCode { get; } = suggestedStatusCode;
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}
