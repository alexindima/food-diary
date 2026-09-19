namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ArchitectureSourceDiscoveryTests {
    [Theory]
    [InlineData("IExampleRepository", true)]
    [InlineData("IExampleStore", true)]
    [InlineData("string", false)]
    public void ForeignRepositoryGuard_DetectsViolationsInSiblingAbstractions(string dependency, bool violates) {
        DirectoryInfo root = Directory.CreateTempSubdirectory("fooddiary-repository-guard-");
        try {
            foreach (string folder in new[] { "Owner/Application.Abstractions", "Owner/Application", "Consumer/Application" }) {
                Directory.CreateDirectory(Path.Combine(root.FullName, folder));
            }
            File.WriteAllText(Path.Combine(root.FullName, "Owner/Application.Abstractions/Ports.cs"),
                "public interface IExampleRepository { } public interface IExampleStore { }");
            File.WriteAllText(Path.Combine(root.FullName, "Owner/Application/Handler.cs"),
                "public class OwnerHandler(IExampleRepository repository) { }");
            File.WriteAllText(Path.Combine(root.FullName, "Consumer/Application/Handler.cs"),
                $"public class ConsumerHandler({dependency} repository) {{ }}");
            string[] violations = ApplicationConsumerBoundaryTests.FindForeignRepositoryUsages(root.FullName);
            if (violates) {
                Assert.Contains(dependency, Assert.Single(violations), StringComparison.Ordinal);
            } else {
                Assert.Empty(violations);
            }
        } finally {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void ForeignRepositoryGuard_RejectsEmptyDiscovery() {
        DirectoryInfo root = Directory.CreateTempSubdirectory("fooddiary-empty-guard-");
        try {
            Assert.Throws<Xunit.Sdk.NotEmptyException>(() =>
                ApplicationConsumerBoundaryTests.FindForeignRepositoryUsages(root.FullName));
        } finally {
            root.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("FoodDiary.MailInbox.Internal")]
    [InlineData("System.Net.Mail")]
    public void ProductionRoots_ScanRelocatedModuleAndSharedSources(string forbiddenNamespace) {
        DirectoryInfo root = Directory.CreateTempSubdirectory("fooddiary-roots-");
        try {
            string[] folders = ["Modules/Example/Application", "Shared/FoodDiary.Example.Contracts"];
            foreach (string folder in folders) {
                string directory = Path.Combine(root.FullName, folder);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, Path.GetFileName(directory) + ".csproj"), "<Project />");
                File.WriteAllText(Path.Combine(directory, "Consumer.cs"), $"using {forbiddenNamespace};");
            }

            IReadOnlyDictionary<string, string> roots = ProjectReferenceReader.ReadProductionProjectRoots(root.FullName);
            Assert.Equal(folders.Select(folder => Path.GetFullPath(Path.Combine(root.FullName, folder))).Order(StringComparer.Ordinal),
                roots.Values.Order(StringComparer.Ordinal), StringComparer.Ordinal);
            Assert.Equal(2, SourceScanner.FindLinePatternViolations(roots.Values, [forbiddenNamespace], requireSourceRoot: true).Length);
            Assert.Throws<DirectoryNotFoundException>(() => SourceScanner.FindLinePatternViolations(
                Path.Combine(root.FullName, "Missing"), [forbiddenNamespace], requireSourceRoot: true));
        } finally {
            root.Delete(recursive: true);
        }
    }

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
