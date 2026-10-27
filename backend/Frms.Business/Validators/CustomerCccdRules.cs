using Frms.Business.Exceptions;

namespace Frms.Business.Validators;

/// <summary>OD-09/10: CCCD is optional in profile; a nonblank value must be 12 ASCII digits.</summary>
public static class CustomerCccdRules
{
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValid(string? value)
    {
        if (value is not { Length: 12 }) return false;
        foreach (var character in value)
            if (character is < '0' or > '9') return false;
        return true;
    }

    public static BusinessException InvalidField() =>
        FieldError("National ID / CCCD must contain exactly 12 digits.",
            "Enter exactly 12 digits (0–9) for your National ID / CCCD.");

    public static BusinessException DuplicateField() =>
        FieldError("The National ID / CCCD is already registered.",
            "This National ID / CCCD is already registered.");

    public static BusinessException ReservationError() =>
        FieldError("Complete your profile to continue your reservation.",
            "Add a valid 12-digit National ID / CCCD to continue your reservation.");

    private static BusinessException FieldError(string message, string fieldMessage) =>
        new("VALIDATION_ERROR", message, 400,
            new Dictionary<string, string[]> { ["cccd"] = new[] { fieldMessage } });
}
