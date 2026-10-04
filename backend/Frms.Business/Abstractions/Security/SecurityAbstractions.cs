namespace Frms.Business.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    Models.Results.IssuedToken Generate(Guid userAccountId, string role);
}
