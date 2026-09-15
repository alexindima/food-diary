namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class ApplicationModuleStructureTests {
    [Fact]
    public void ApplicationModules_DoNotRepeatModuleNameAsRootFolder() {
        string[] violations = [.. ApplicationModuleDirectories()
            .Select(projectDirectory => new {
                ProjectDirectory = projectDirectory,
                ModuleName = Path.GetFileName(Path.GetDirectoryName(projectDirectory)!),
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
        string namespaceRoot = Path.GetFileNameWithoutExtension(Assert.Single(Directory.GetFiles(projectDirectory, "*.csproj")));

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

    private static string[] ApplicationModuleDirectories() {
        string[] directories = [.. ModuleSourceCatalog.ApplicationRoots.Values.Order(StringComparer.Ordinal)];
        Assert.NotEmpty(directories);
        return directories;
    }
}
