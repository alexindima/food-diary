namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class ApplicationModuleStructureTests {
    [Fact]
    public void ApplicationModules_DoNotRepeatModuleNameAsRootFolder() {
        string[] violations = [.. ApplicationModuleDirectories()
            .Select(projectDirectory => new {
                ProjectDirectory = projectDirectory,
                ModuleName = Path.GetFileName(projectDirectory)["FoodDiary.Application.".Length..],
            })
            .Select(module => Path.Combine(module.ProjectDirectory, module.ModuleName))
            .Where(Directory.Exists)
            .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Application module projects must not repeat their module name as a root folder. " +
            "Place Commands, Queries, and other purpose folders directly under the project root:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationModuleNamespaces_MatchProjectFolderStructure() {
        string[] violations = [.. ApplicationModuleDirectories()
            .SelectMany(projectDirectory => NamespaceViolations(projectDirectory))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Application module namespaces must match their paths relative to the project root:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> NamespaceViolations(string projectDirectory) {
        string namespaceRoot = Path.GetFileName(projectDirectory);

        foreach (string sourceFile in SourceScanner.SourceFiles(projectDirectory)) {
            string? actualNamespace = CSharpSyntaxReader.ReadNamespace(sourceFile);
            if (actualNamespace is null && IsNamespaceOptional(sourceFile)) {
                continue;
            }

            string relativePath = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, sourceFile);
            string relativeDirectory =
                Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, sourceFile)) ?? string.Empty;
            string namespaceSuffix = relativeDirectory
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');
            string expectedNamespace = string.IsNullOrWhiteSpace(namespaceSuffix)
                ? namespaceRoot
                : $"{namespaceRoot}.{namespaceSuffix}";

            if (!string.Equals(expectedNamespace, actualNamespace, StringComparison.Ordinal)) {
                yield return $"{relativePath}: expected '{expectedNamespace}', found '{actualNamespace ?? "<none>"}'";
            }
        }
    }

    private static bool IsNamespaceOptional(string sourceFile) =>
        Path.GetFileName(sourceFile) is "AssemblyInfo.cs" or "GlobalUsings.cs" or "Program.cs";

    private static string[] ApplicationModuleDirectories() =>
        [.. Directory.GetDirectories(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application.*")
            .Where(projectDirectory => File.Exists(Path.Combine(
                projectDirectory,
                $"{Path.GetFileName(projectDirectory)}.csproj")))
            .Order(StringComparer.Ordinal)];
}
