using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DockerfileDependencyTests {
    [Theory]
    [InlineData(@"..\FoodDiary.Application.Runtime\FoodDiary.Application.Runtime.csproj")]
    [InlineData("../FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj")]
    public void ProjectReferencePaths_AreNormalizedForCurrentOperatingSystem(string projectReference) {
        string separator = Path.DirectorySeparatorChar.ToString();
        string expected = string.Join(separator, "..", "FoodDiary.Application.Runtime", "FoodDiary.Application.Runtime.csproj");

        Assert.Equal(expected, NormalizeProjectReferencePath(projectReference));
    }

    [Fact]
    public void DotNetDockerfiles_CopyAllTransitiveProjectReferencesBeforeRestoreAndPublish() {
        string root = GetRepositoryRoot();
        string[] violations = [.. GetDotNetDockerfiles(root)
            .SelectMany(dockerfile => FindMissingCopies(root, dockerfile))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void DotNetDockerfiles_CopySharedCompilerAdditionalFilesBeforePublish() {
        string root = GetRepositoryRoot();
        const string rootPrefix = "$(MSBuildThisFileDirectory)";
        string[] compilerInputs = [.. XDocument.Load(Path.Combine(root, "Directory.Build.props"))
            .Descendants("AdditionalFiles")
            .Select(static element => (string?)element.Attribute("Include"))
            .OfType<string>()];
        Assert.NotEmpty(compilerInputs);

        foreach (string input in compilerInputs) {
            Assert.StartsWith(rootPrefix, input, StringComparison.Ordinal);
            string relativeInput = input[rootPrefix.Length..].Replace('\\', '/');
            Assert.True(File.Exists(Path.Combine(root, relativeInput)), $"Compiler input does not exist: {relativeInput}");
            string directory = Path.GetDirectoryName(relativeInput)!.Replace('\\', '/');
            string destination = directory.Length == 0 ? "./" : directory + "/";
            string expectedCopy = $"COPY {relativeInput} {destination}";
            foreach (string dockerfile in GetDotNetDockerfiles(root)) {
                string[] lines = File.ReadAllLines(dockerfile);
                int publishLine = Array.FindIndex(lines, static line => line.StartsWith("RUN dotnet publish ", StringComparison.Ordinal));
                Assert.True(publishLine >= 0, $"{Path.GetRelativePath(root, dockerfile)} has no publish command.");
                Assert.True(
                    lines.Take(publishLine).Any(line => string.Equals(line.Trim(), expectedCopy, StringComparison.Ordinal)),
                    $"{Path.GetRelativePath(root, dockerfile)} must copy compiler input before publish: {expectedCopy}");
            }
        }
    }

    private static IEnumerable<string> GetDotNetDockerfiles(string root) =>
        Directory.GetFiles(root, "Dockerfile", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}FoodDiary.Web.Client{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(static path => Directory.GetFiles(Path.GetDirectoryName(path)!, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0);

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
