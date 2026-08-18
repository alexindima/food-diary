using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BuildWorkflowGuardrailTests {
    [Fact]
    public void CiAndPrePush_RunAllBackendTestsThroughSolution() {
        string ciWorkflow = File.ReadAllText(ArchitectureTestPaths.FromRoot(".github", "workflows", "ci-tests.yml"));
        string prePush = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-push"));

        Assert.Multiple(
            () => Assert.Contains("dotnet test FoodDiary.slnx", ciWorkflow, StringComparison.Ordinal),
            () => Assert.DoesNotContain("tests/*/*.csproj", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("--maxcpucount:1", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("dotnet test FoodDiary.slnx", prePush, StringComparison.Ordinal),
            () => Assert.Contains("--maxcpucount:1", prePush, StringComparison.Ordinal));
    }

    [Fact]
    public void Solution_IncludesEveryRunnableTestProject() {
        var solution = XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));
        var solutionProjects = solution.Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] missingTestProjects = [.. ProjectReferenceReader.ReadTestProjectNames()
            .Where(project => !string.Equals(project, "FoodDiary.Testing", StringComparison.Ordinal))
            .Where(project => !solutionProjects.Contains(project))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(missingTestProjects);
    }

    [Fact]
    public void SuccessfulGitHooks_RemoveGeneratedDotnetArtifacts() {
        string preCommit = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-commit"));
        string prePush = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-push"));

        Assert.Multiple(
            () => Assert.True(CountOccurrences(preCommit, "Clean-NestedDotnetArtifacts.ps1 -IncludeRoot") >= 2),
            () => Assert.True(CountOccurrences(prePush, "Clean-NestedDotnetArtifacts.ps1 -IncludeRoot") >= 2));
    }

    [Fact]
    public void DevelopmentMcpLauncher_LocksActiveSessionsAndCollectsStaleOnes() {
        string launcher = File.ReadAllText(ArchitectureTestPaths.FromRoot("scripts", "Start-FoodDiaryDevelopmentMcp.ps1"));

        Assert.Multiple(
            () => Assert.Contains("Remove-StaleSessionDirectories", launcher, StringComparison.Ordinal),
            () => Assert.Contains(".session.lock", launcher, StringComparison.Ordinal),
            () => Assert.Contains("[IO.FileShare]::None", launcher, StringComparison.Ordinal),
            () => Assert.Contains("^(?:[0-9a-fA-F]{32}|[0-9a-f]{64})$", launcher, StringComparison.Ordinal));
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;
}
