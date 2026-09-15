namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PhysicalProjectLayoutTests {
    // Existing physical nesting only. Remove entries as projects move; do not add new exceptions.
    private static readonly string[] LegacyNesting = [];

    [Fact]
    public void AdminPersistenceModel_RemainsInInfrastructureSourceCoverage() {
        Assert.Contains(ArchitectureTestPaths.FromRoot("Modules", "Admin", "PersistenceModel"),
            ModuleSourceCatalog.InfrastructureRoots(), StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Application")]
    [InlineData("Application.Abstractions")]
    [InlineData("Contracts")]
    [InlineData("Domain")]
    [InlineData("Infrastructure")]
    [InlineData("PersistenceModel")]
    [InlineData("Presentation")]
    public void AdminProjects_DoNotRepeatModuleFolders(string project) {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Admin", project);
        Assert.True(Directory.Exists(root));
        Assert.DoesNotContain(Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories),
            directory => string.Equals(Path.GetFileName(directory), "Admin", StringComparison.OrdinalIgnoreCase)
                && !Path.GetRelativePath(root, directory).Split(Path.DirectorySeparatorChar)
                    .Any(segment => segment is "bin" or "obj" or ".artifacts"));
    }

    [Fact]
    public void AdminPresentation_DoesNotWrapTheWholeProjectInFeatures() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Admin", "Presentation");
        Assert.False(Directory.Exists(Path.Combine(root, "Features")));
        Assert.Empty(Directory.EnumerateFiles(root, "*Controller.cs"));
        Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(root, "Controllers"), "*Controller.cs"));
    }

    [Fact]
    public void AdminAbstractions_DoNotRepeatModuleFolder() {
        string projectDirectory = ArchitectureTestPaths.FromRoot("Modules", "Admin", "Application.Abstractions");
        Assert.DoesNotContain(Directory.EnumerateDirectories(projectDirectory),
            directory => string.Equals(Path.GetFileName(directory), "Admin", StringComparison.OrdinalIgnoreCase));
        Assert.True(Directory.Exists(Path.Combine(projectDirectory, "Common")));
        Assert.True(Directory.Exists(Path.Combine(projectDirectory, "Models")));
    }

    [Fact]
    public void Projects_DoNotIntroducePhysicalNesting() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] projects = [.. EnumerateProjectPaths(root)];
        string[] actual = FindNestedProjects(projects);
        string[] unexpected = [.. actual.Except(LegacyNesting, StringComparer.OrdinalIgnoreCase)];
        string[] stale = [.. LegacyNesting.Except(actual, StringComparer.OrdinalIgnoreCase)];
        Assert.Multiple(
            () => Assert.True(unexpected.Length == 0, "Move nested projects beside their parent projects:\n" + string.Join('\n', unexpected)),
            () => Assert.True(stale.Length == 0, "Remove resolved legacy nesting entries:\n" + string.Join('\n', stale)));
    }

    [Theory]
    [InlineData("Module/Application/P.csproj", "Module/Application/Abstractions/C.csproj", true)]
    [InlineData("Module/Application/P.csproj", "Module/Application.Abstractions/C.csproj", false)]
    [InlineData("Module/Application/P.csproj", "Module/Application/C.csproj", false)]
    [InlineData("Module/Application/P.csproj", "Module/ApplicationExtra/C.csproj", false)]
    [InlineData("Module/P.csproj", "Module/tests/Child/C.csproj", true)]
    [InlineData("P.csproj", "Child/C.csproj", true)]
    [InlineData("Module/Application/P.csproj", "module/application/Child/C.csproj", true)]
    public void NestingDetection_UsesPhysicalDirectoryBoundaries(string parent, string child, bool expected) {
        Assert.Equal(expected, FindNestedProjects([parent, child]).Length != 0);
    }

    private static string[] FindNestedProjects(string[] projects) =>
        [.. projects.SelectMany(parent => projects
            .Where(child => !string.Equals(parent, child, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Path.GetDirectoryName(parent), Path.GetDirectoryName(child), StringComparison.OrdinalIgnoreCase)
                && child.StartsWith(DirectoryPrefix(parent), StringComparison.OrdinalIgnoreCase))
            .Select(child => $"{parent} -> {child}"))
            .Order(StringComparer.OrdinalIgnoreCase)];

    private static string DirectoryPrefix(string path) {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? string.Empty : path[..(separator + 1)];
    }

    private static IEnumerable<string> EnumerateProjectPaths(string root) {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out string? directory)) {
            foreach (string project in Directory.EnumerateFiles(directory, "*.csproj")) {
                yield return Path.GetRelativePath(root, project).Replace('\\', '/');
            }
            foreach (string child in Directory.EnumerateDirectories(directory)) {
                string name = Path.GetFileName(child);
                if (name is ".git" or ".artifacts" or "bin" or "obj" or "node_modules" or ".angular"
                    || File.GetAttributes(child).HasFlag(FileAttributes.ReparsePoint)) {
                    continue;
                }
                pending.Push(child);
            }
        }
    }
}
