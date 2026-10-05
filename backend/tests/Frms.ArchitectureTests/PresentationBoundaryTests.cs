using Frms.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Frms.ArchitectureTests;

public sealed class PresentationBoundaryTests
{
    [Test]
    public void ControllersDoNotDependOnDataAccessTypes()
    {
        var controllerTypes = typeof(ScaffoldControllerBase).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .ToArray();

        var forbiddenDependencies = controllerTypes
            .SelectMany(GetPublicAndInjectedTypes)
            .Where(IsDataAccessType)
            .Select(type => type.FullName)
            .Distinct()
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controllerTypes, Is.Not.Empty);
            Assert.That(forbiddenDependencies, Is.Empty);
        });
    }

    [Test]
    public void BackgroundJobsDoNotDependOnDataAccessOrDbContext()
    {
        var backgroundJobTypes = typeof(ScaffoldControllerBase).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith("Frms.Api.BackgroundJobs", StringComparison.Ordinal) == true)
            .ToArray();

        var dependencies = backgroundJobTypes
            .SelectMany(GetPublicAndInjectedTypes)
            .Where(IsDataAccessType)
            .Select(type => type.FullName)
            .Distinct()
            .ToArray();

        Assert.That(dependencies, Is.Empty);
    }

    private static IEnumerable<Type> GetPublicAndInjectedTypes(Type type)
    {
        return type.GetConstructors().SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
            .Concat(type.GetFields().Select(field => field.FieldType))
            .Concat(type.GetProperties().Select(property => property.PropertyType))
            .Concat(type.GetMethods().SelectMany(method =>
                method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)));
    }

    private static bool IsDataAccessType(Type type)
    {
        var candidate = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return typeof(DbContext).IsAssignableFrom(candidate)
            || candidate.Namespace?.StartsWith("Frms.DataAccess", StringComparison.Ordinal) == true;
    }
}
