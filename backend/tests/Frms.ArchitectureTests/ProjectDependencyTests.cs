using System.Xml.Linq;

namespace Frms.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    [Test]
    public void BusinessReferencesOnlyDataAccessAmongFrmsProductionProjects()
    {
        var references = GetFrmsProjectReferences("Frms.Business");

        Assert.That(references, Is.EqualTo(new[] { "Frms.DataAccess" }));
    }

    [Test]
    public void DataAccessReferencesNoFrmsProductionProject()
    {
        var references = GetFrmsProjectReferences("Frms.DataAccess");

        Assert.That(references, Is.Empty);
    }

    [Test]
    public void InfrastructureReferencesOnlyBusinessAmongFrmsProductionProjects()
    {
        var references = GetFrmsProjectReferences("Frms.Infrastructure");

        Assert.That(references, Is.EqualTo(new[] { "Frms.Business" }));
    }

    [Test]
    public void ApiCompositionRootReferencesBusinessDataAccessAndInfrastructure()
    {
        var references = GetFrmsProjectReferences("Frms.Api");

        Assert.That(
            references,
            Is.EquivalentTo(new[] { "Frms.Business", "Frms.DataAccess", "Frms.Infrastructure" }));
    }

    private static string[] GetFrmsProjectReferences(string projectName)
    {
        var backendDirectory = FindBackendDirectory();
        var projectPath = Path.Combine(backendDirectory, projectName, $"{projectName}.csproj");
        var document = XDocument.Load(projectPath);

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Select(value => Path.GetFileNameWithoutExtension(value!))
            .Where(value => value.StartsWith("Frms.", StringComparison.Ordinal))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindBackendDirectory()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Frms.slnx")))
            {
                return Path.Combine(current.FullName, "backend");
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the backend directory.");
    }
}
