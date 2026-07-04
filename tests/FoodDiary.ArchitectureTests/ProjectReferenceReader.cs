using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class ProjectReferenceReader {
    public static IReadOnlyDictionary<string, string[]> ReadProductionProjectReferences() =>
        ReadProductionProjectPaths()
            .ToDictionary(
                GetProjectNameFromPath,
                path => ReadProjectReferences(path).Order(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

    public static IReadOnlyList<string> ReadProductionProjectNames() =>
        ReadProductionProjectPaths()
            .Select(GetProjectNameFromPath)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public static string[] ReadProjectReferences(string relativeProjectPath) {
        string projectPath = ArchitectureTestPaths.FromRoot(
            relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return [.. document.Descendants("ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => GetProjectNameFromReference(value!))
            .Order(StringComparer.Ordinal)];
    }

    public static string[] ReadPackageReferences(string relativeProjectPath) {
        string projectPath = ArchitectureTestPaths.FromRoot(
            relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return [.. document.Descendants("PackageReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Order(StringComparer.Ordinal)];
    }

    private static IEnumerable<string> ReadProductionProjectPaths() =>
        Directory.GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Order(StringComparer.Ordinal);

    private static string GetProjectNameFromPath(string projectPath) =>
        Path.GetFileNameWithoutExtension(projectPath);

    private static string GetProjectNameFromReference(string includeValue) {
        string normalized = includeValue.Replace('\\', '/');
        string fileName = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Path.GetFileNameWithoutExtension(fileName);
    }
}
