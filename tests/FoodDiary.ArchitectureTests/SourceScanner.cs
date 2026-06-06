using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class SourceScanner {
    public static string[] FindLinePatternViolations(
        string sourceRoot,
        IReadOnlyCollection<string> forbiddenPatterns) {
        if (!Directory.Exists(sourceRoot)) {
            return [];
        }

        string repositoryRoot = ArchitectureTestPaths.RepositoryRoot;

        return [.. Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => ArchitectureTestPaths.IsGeneratedOrBuildPath(path) is false)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = StripLineComment(line) }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}"))
            .OrderBy(static value => value, StringComparer.Ordinal)];
    }

    public static string[] FindLinePatternViolations(
        IEnumerable<string> sourceRoots,
        IReadOnlyCollection<string> forbiddenPatterns) =>
        [.. sourceRoots
            .SelectMany(sourceRoot => FindLinePatternViolations(sourceRoot, forbiddenPatterns))
            .OrderBy(static value => value, StringComparer.Ordinal)];

    public static IEnumerable<string> SourceFiles(string sourceRoot) {
        if (!Directory.Exists(sourceRoot)) {
            return [];
        }

        return Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => ArchitectureTestPaths.IsGeneratedOrBuildPath(path) is false)
            .OrderBy(static path => path, StringComparer.Ordinal);
    }

    public static IEnumerable<string> SourceFiles(IEnumerable<string> sourceRoots) =>
        sourceRoots
            .SelectMany(SourceFiles)
            .OrderBy(static path => path, StringComparer.Ordinal);

    private static string StripLineComment(string line) {
        int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        return commentIndex < 0 ? line : line[..commentIndex];
    }
}
