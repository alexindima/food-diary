namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ArchitectureSourceDiscoveryTests {
    [Theory]
    [InlineData("C:\\FD\\.artifacts\\evidence\\Snapshot.cs", true)]
    [InlineData("C:/FD/.artifacts/evidence/Snapshot.csproj", true)]
    [InlineData(".artifacts/evidence/Snapshot.cs", true)]
    [InlineData("C:/FD/.ARTIFACTS/evidence/Snapshot.cs", true)]
    [InlineData("C:/FD/Modules/Example/Domain/bin/Debug/Source.cs", true)]
    [InlineData("C:\\FD\\Modules\\Example\\Domain\\obj\\Source.cs", true)]
    [InlineData("Modules/Example/Source.Designer.cs", true)]
    [InlineData("Modules/Example/Source.g.cs", true)]
    [InlineData("Modules/Example/Source.AssemblyInfo.cs", true)]
    [InlineData("C:/FD/Modules/Example/Domain/Source.cs", false)]
    [InlineData("C:/FD/.artifacts-like/Source.cs", false)]
    [InlineData("C:/FD/Modules/Example/Domain/.artifacts.cs", false)]
    [InlineData("C:/FD/.other/Source.cs", false)]
    public void GeneratedPathClassification_RespectsDirectoryBoundaries(string path, bool expected) {
        Assert.Equal(expected, ArchitectureTestPaths.IsGeneratedOrBuildPath(path));
    }

    [Fact]
    public void RepositoryScans_ExcludeEvidenceButRetainSourcesAndRealEventViolations() {
        string root = Path.Combine(Path.GetTempPath(), "FoodDiaryArchitecture-" + Guid.NewGuid().ToString("N"));
        try {
            foreach (string directory in new[] { ".artifacts/evidence", "Modules/Example/Domain", ".artifacts-like" }) {
                string absolute = Path.Combine(root, directory);
                Directory.CreateDirectory(absolute);
                File.WriteAllText(Path.Combine(absolute, "BadEvent.cs"), "public sealed record BadEvent : IIntegrationEvent;");
                string projectName = directory.Replace('/', '.') + ".csproj";
                File.WriteAllText(Path.Combine(absolute, projectName), "<Project />");
            }

            string[] expectedSources = [
                Path.Combine(root, ".artifacts-like", "BadEvent.cs"),
                Path.Combine(root, "Modules", "Example", "Domain", "BadEvent.cs"),
            ];
            string[] sources = [.. SourceScanner.SourceFiles(root)];
            string[] violations = EventGovernanceTests.FindIntegrationEventViolations(root);

            Assert.Multiple(
                () => Assert.Equal(expectedSources.Order(StringComparer.Ordinal), sources, StringComparer.Ordinal),
                () => Assert.Equal([".artifacts-like", "Modules.Example.Domain"],
                    ProjectReferenceReader.ReadProductionProjectNames(root)),
                () => Assert.Equal(2, SourceScanner.FindLinePatternViolations(root, ["IIntegrationEvent"]).Length),
                () => Assert.Equal(expectedSources.Select(path => Path.GetRelativePath(root, path) + ":1")
                    .Order(StringComparer.Ordinal), violations, StringComparer.Ordinal));
        } finally {
            if (Directory.Exists(root)) {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
