using System.Text.Json;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RepositoryKnowledgeGuardrailTests {
    [Fact]
    public void RootGuide_LinksEveryScopedAgentGuide() {
        string rootGuide = File.ReadAllText(ArchitectureTestPaths.FromRoot("AGENTS.md"));
        string[] scopedGuides = [.. Directory
            .EnumerateFiles(ArchitectureTestPaths.RepositoryRoot, "AGENTS.md", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}.artifacts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path).Replace('\\', '/'))
            .Where(static path => !string.Equals(path, "AGENTS.md", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        string[] missingLinks = [.. scopedGuides.Where(path => !rootGuide.Contains($"`{path}`", StringComparison.Ordinal))];

        Assert.True(missingLinks.Length == 0, $"Root AGENTS.md does not link scoped guide(s): {string.Join(", ", missingLinks)}");
    }

    [Fact]
    public void RepositoryCatalog_ContainsExactlyTheSolutionProjects() {
        var solution = XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));
        string[] solutionProjects = [.. solution
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value.Replace('\\', '/'))
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(static path => path!)
            .Order(StringComparer.Ordinal)];

        using var catalog = JsonDocument.Parse(File.ReadAllText(
            ArchitectureTestPaths.FromRoot(".llm-wiki", "generated", "repository-catalog.json")));
        string[] catalogProjects = [.. catalog.RootElement
            .GetProperty("dotnet")
            .GetProperty("projects")
            .EnumerateArray()
            .Select(static project => project.GetProperty("path").GetString())
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(static path => path!)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(solutionProjects, catalogProjects, StringComparer.Ordinal);
    }
}
