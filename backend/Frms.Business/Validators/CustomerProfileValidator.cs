using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;

namespace Frms.Business.Validators;

public static class CustomerProfileValidator
{
    public static UpdateCustomerProfileCommand ValidateAndNormalize(UpdateCustomerProfileCommand command)
    {
        if (!command.UpdateFullName && !command.UpdateAddress && !command.UpdateCccd)
            throw Invalid();

        var name = command.FullName?.Trim();
        var address = command.Address?.Trim();
        var cccd = command.Cccd?.Trim();
        if (command.UpdateFullName && (string.IsNullOrWhiteSpace(name) || name.Length > 150))
            throw Invalid();
        if (address?.Length > 1000 || cccd?.Length > 20)
            throw Invalid();

        return command with
        {
            FullName = name,
            Address = string.IsNullOrEmpty(address) ? null : address,
            Cccd = string.IsNullOrEmpty(cccd) ? null : cccd
        };
    }

    private static BusinessException Invalid() =>
        new("VALIDATION_ERROR", "Customer profile fields are invalid.", 400);
}
