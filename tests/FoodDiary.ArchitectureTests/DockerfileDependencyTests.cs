using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DockerfileDependencyTests {
    [Theory]
    [InlineData(@"..\FoodDiary.Application\FoodDiary.Application.csproj")]
    [InlineData("../FoodDiary.Application/FoodDiary.Application.csproj")]
    public void ProjectReferencePaths_AreNormalizedForCurrentOperatingSystem(string projectReference) {
        string separator = Path.DirectorySeparatorChar.ToString();
        string expected = string.Join(separator, "..", "FoodDiary.Application", "FoodDiary.Application.csproj");

        Assert.Equal(expected, NormalizeProjectReferencePath(projectReference));
    }

    [Fact]
    public void DotNetDockerfiles_CopyAllTransitiveProjectReferencesBeforeRestoreAndPublish() {
        string root = GetRepositoryRoot();
        string[] violations = [.. Directory.GetFiles(root, "Dockerfile", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}FoodDiary.Web.Client{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(dockerfile => FindMissingCopies(root, dockerfile))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FindMissingCopies(string root, string dockerfile) {
        string projectDirectory = Path.GetDirectoryName(dockerfile)!;
        string projectFile = Directory.GetFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).Single();
        string docker = File.ReadAllText(dockerfile);

        foreach (string dependency in GetTransitiveProjectReferences(projectFile)) {
            string relativeDirectory = Path.GetRelativePath(root, Path.GetDirectoryName(dependency)!).Replace('\\', '/');
            string projectCopy = $"COPY {relativeDirectory}/*.csproj {relativeDirectory}/";
            string sourceCopy = $"COPY {relativeDirectory}/ {relativeDirectory}/";
            if (!docker.Contains(projectCopy, StringComparison.Ordinal)) {
                yield return $"{Path.GetRelativePath(root, dockerfile)} missing restore copy: {projectCopy}";
            }

            if (!docker.Contains(sourceCopy, StringComparison.Ordinal)) {
                yield return $"{Path.GetRelativePath(root, dockerfile)} missing source copy: {sourceCopy}";
            }
        }
    }

    private static HashSet<string> GetTransitiveProjectReferences(string projectFile) {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Visit(Path.GetFullPath(projectFile), visited);
        visited.Remove(Path.GetFullPath(projectFile));
        return visited;
    }

    private static void Visit(string projectFile, HashSet<string> visited) {
        if (!visited.Add(projectFile)) {
            return;
        }

        var project = XDocument.Load(projectFile);
        foreach (XElement reference in project.Descendants("ProjectReference")) {
            string? include = reference.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(include)) {
                string normalizedInclude = NormalizeProjectReferencePath(include);
                Visit(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectFile)!, normalizedInclude)), visited);
            }
        }
    }

    private static string NormalizeProjectReferencePath(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    private static string GetRepositoryRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            if (File.Exists(Path.Combine(current.FullName, "FoodDiary.slnx"))) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
