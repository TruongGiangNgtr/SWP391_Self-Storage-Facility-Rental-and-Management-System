using Frms.Business.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Frms.Api.Authentication;

internal sealed class BCryptPasswordHasher(IOptions<BCryptOptions> options) : IPasswordHasher
{
    private readonly int _workFactor = options.Value.WorkFactor;
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    public bool Verify(string password, string passwordHash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, passwordHash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}
