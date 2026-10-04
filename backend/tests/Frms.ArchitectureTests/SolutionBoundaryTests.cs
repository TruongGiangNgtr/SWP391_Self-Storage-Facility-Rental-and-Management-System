namespace Frms.ArchitectureTests;

public sealed class SolutionBoundaryTests
{
    [Test]
    public void SolutionDoesNotContainWorkerProject()
    {
        var solutionPath = FindSolutionPath();
        var solutionText = File.ReadAllText(solutionPath);

        Assert.That(solutionText, Does.Not.Contain("Frms.Worker"));
    }

    private static string FindSolutionPath()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Frms.sln");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate backend/Frms.sln from the test output directory.");
    }
}
