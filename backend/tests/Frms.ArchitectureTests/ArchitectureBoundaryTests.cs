using System.Reflection;
using Frms.Api.Controllers;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence;
namespace Frms.ArchitectureTests;

[TestFixture]
public sealed class ArchitectureBoundaryTests
{
    [Test]
    public void Business_DoesNotReferenceApiOrInfrastructure() { var references = typeof(IAuthenticationService).Assembly.GetReferencedAssemblies().Select(name => name.Name); Assert.That(references, Does.Not.Contain("Frms.Api").And.Not.Contain("Frms.Infrastructure")); }
    [Test]
    public void DataAccess_DoesNotReferenceApiOrInfrastructure() { var references = typeof(FrmsDbContext).Assembly.GetReferencedAssemblies().Select(name => name.Name); Assert.That(references, Does.Not.Contain("Frms.Api").And.Not.Contain("Frms.Infrastructure")); }
    [Test]
    public void Infrastructure_DoesNotReferenceDataAccessOrApi() { var references = Assembly.Load("Frms.Infrastructure").GetReferencedAssemblies().Select(name => name.Name); Assert.That(references, Does.Not.Contain("Frms.DataAccess").And.Not.Contain("Frms.Api")); }
    [Test]
    public void Controllers_DependOnBusinessServicesNotRepositoryOrDbContext()
    {
        var parameters = typeof(AuthController).Assembly.GetTypes().Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal)).SelectMany(type => type.GetConstructors()).SelectMany(ctor => ctor.GetParameters()).Select(parameter => parameter.ParameterType.FullName ?? string.Empty).ToArray();
        Assert.Multiple(() => { Assert.That(parameters, Has.Some.EqualTo(typeof(IAuthenticationService).FullName)); Assert.That(parameters, Has.None.Contains("Frms.DataAccess")); Assert.That(parameters, Has.None.Contains("DbContext")); });
    }
    [Test]
    public void BusinessServiceContracts_DoNotExposeApiDtosOrEfEntities()
    {
        var exposed = typeof(IAuthenticationService).Assembly.GetTypes().Where(type => type.IsInterface && type.Namespace == "Frms.Business.Services.Interfaces").SelectMany(type => type.GetMethods()).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)).SelectMany(Flatten).Select(type => type.FullName ?? string.Empty).ToArray();
        Assert.That(exposed, Has.None.StartsWith("Frms.Api.DTOs").And.None.StartsWith("Frms.DataAccess.Persistence.Entities"));
    }
    [Test]
    public void ApiBackgroundJobs_DoNotDependOnDataAccessOrDbContext()
    {
        var dependencies = typeof(AuthController).Assembly.GetTypes().Where(type => type.Namespace == "Frms.Api.BackgroundJobs").SelectMany(type => type.GetConstructors().SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType)).Concat(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(field => field.FieldType))).Select(type => type.FullName ?? string.Empty).ToArray();
        Assert.That(dependencies, Has.None.Contains("Frms.DataAccess").And.None.Contains("DbContext"));
    }
    private static IEnumerable<Type> Flatten(Type type) { yield return type; foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : []) foreach (var nested in Flatten(argument)) yield return nested; }
}
