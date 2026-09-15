namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RequestFolderConventionTests {
    [Fact]
    public void ApplicationCommandsAndQueries_LiveInDedicatedFeatureFolders() {
        string repositoryRoot = ArchitectureTestPaths.RepositoryRoot;
        string[] applicationRoots = [.. Directory
            .GetFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Where(static path => {
                string projectName = Path.GetFileNameWithoutExtension(path);
                return projectName.Contains(".Application", StringComparison.Ordinal) &&
                    !projectName.EndsWith(".Tests", StringComparison.Ordinal);
            })
            .Select(static path => Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Application project has no directory."))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoots)
            .Where(static path => Path.GetDirectoryName(path) is { } directory &&
                Path.GetFileName(directory) is "Commands" or "Queries")
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Place every command or query slice in its own feature folder under Commands/ or Queries/:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }
}
