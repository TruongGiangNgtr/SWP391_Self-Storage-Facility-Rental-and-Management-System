namespace Frms.Business.Exceptions;

public sealed class BusinessException(string code, string safeMessage, int suggestedStatusCode) : Exception(safeMessage)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int SuggestedStatusCode { get; } = suggestedStatusCode;
}
