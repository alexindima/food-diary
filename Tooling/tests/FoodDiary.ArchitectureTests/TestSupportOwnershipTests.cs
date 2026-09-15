using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TestSupportOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Application.Runtime.Tests")]
    [InlineData("FoodDiary.Application.Contracts.Tests")]
    [InlineData("FoodDiary.Email.Contracts.Tests")]
    public void SharedApplicationTests_KeepBusinessModulesOutOfTheirDependencyClosure(string project) {
        var graph = ProjectReferenceReader.ReadProductionProjectReferences()
            .Concat(ProjectReferenceReader.ReadTestProjectReferences())
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(project);

        while (pending.TryPop(out string? current)) {
            if (!visited.Add(current)) {
                continue;
            }

            Assert.False(current.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal),
                $"{project} depends on business module assembly {current}.");
            Assert.True(graph.TryGetValue(current, out string[]? references), $"Missing project {current}.");
            foreach (string reference in references) {
                pending.Push(reference);
            }
        }
    }

    [Fact]
    public void RetiredApplicationTestProject_IsNotRecreatedOrUsedAsLinkedSource() {
        string[] projects = [.. RepositoryFileDiscovery.EnumerateFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj")
            .Where(path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))];

        Assert.Multiple(
            () => Assert.DoesNotContain(projects, path => string.Equals(Path.GetFileName(path), "FoodDiary.Application.Tests.csproj", StringComparison.Ordinal)),
            () => Assert.All(projects, path => Assert.DoesNotContain(XDocument.Load(path).Descendants("Compile"),
                item => ((string?)item.Attribute("Include"))?.Contains("FoodDiary.Application.Tests", StringComparison.Ordinal) == true)));
    }

    [Fact]
    public void TestSupport_RemainsOutsideProductionDependencyGraph() {
        Assert.Multiple(
            () => Assert.Contains("FoodDiary.Testing", ProjectReferenceReader.ReadTestProjectNames(), StringComparer.Ordinal),
            () => Assert.DoesNotContain("FoodDiary.Testing", ProjectReferenceReader.ReadProductionProjectNames(), StringComparer.Ordinal),
            () => Assert.All(ProjectReferenceReader.ReadProductionProjectReferences().Values,
                references => Assert.DoesNotContain("FoodDiary.Testing", references, StringComparer.Ordinal)));
    }

    [Fact]
    public void TestConfiguration_HasOneOwnerOutsideLegacyTests() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string commonSettings = ArchitectureTestPaths.FromRoot("Tooling", "Testing", "TestProjects.props");
        string[] obsoleteImports = [.. RepositoryFileDiscovery.EnumerateFiles(root, "*.props")
            .Where(path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .SelectMany(path => XDocument.Load(path).Descendants("Import")
                .Select(import => (string?)import.Attribute("Project"))
                .Where(value => value?.Contains("$(", StringComparison.Ordinal) == false)
                .Select(value => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, value!.Replace('\\', '/'))))
                .Where(target => string.Equals(target, ArchitectureTestPaths.FromRoot("tests", "Directory.Build.props"), StringComparison.OrdinalIgnoreCase))
                .Select(_ => Path.GetRelativePath(root, path)))];

        Assert.Multiple(
            () => Assert.Empty(obsoleteImports),
            () => Assert.True(File.Exists(commonSettings)),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Tooling", "Testing", "test.runsettings"))),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Tooling", "Testing", "xunit.runner.json"))),
            () => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("tests", "test.runsettings"))),
            () => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("tests", "xunit.runner.json"))));
    }
}
