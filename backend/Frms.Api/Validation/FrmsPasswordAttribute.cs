using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Frms.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FrmsPasswordAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string password) return false;
        return password.Length is >= 8 and <= 64 && Encoding.UTF8.GetByteCount(password) <= 72;
    }

    public override string FormatErrorMessage(string name) => $"{name} must contain 8 to 64 characters and no more than 72 UTF-8 bytes.";
}
