using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class ProjectReferenceReader {
    public static IReadOnlyDictionary<string, string[]> ReadProductionProjectReferences() =>
        ReadProductionProjectPaths()
            .ToDictionary(
                GetProjectNameFromPath,
                path => ReadProjectReferences(path).OrderBy(static name => name, StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

    public static IReadOnlyList<string> ReadProductionProjectNames() =>
        ReadProductionProjectPaths()
            .Select(GetProjectNameFromPath)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

    public static string[] ReadProjectReferences(string relativeProjectPath) {
        var projectPath = ArchitectureTestPaths.FromRoot(
            relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => GetProjectNameFromReference(value!))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> ReadProductionProjectPaths() =>
        Directory.GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => ArchitectureTestPaths.IsGeneratedOrBuildPath(path) is false)
            .OrderBy(static path => path, StringComparer.Ordinal);

    private static string GetProjectNameFromPath(string projectPath) =>
        Path.GetFileNameWithoutExtension(projectPath);

    private static string GetProjectNameFromReference(string includeValue) {
        var normalized = includeValue.Replace('\\', '/');
        var fileName = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Path.GetFileNameWithoutExtension(fileName);
    }
}
