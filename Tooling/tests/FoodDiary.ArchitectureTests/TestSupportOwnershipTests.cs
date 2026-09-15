using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TestSupportOwnershipTests {
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
