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
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = StripLineComment(line) }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];
    }

    public static string[] FindLinePatternViolations(
        IEnumerable<string> sourceRoots,
        IReadOnlyCollection<string> forbiddenPatterns) =>
        [.. sourceRoots
            .SelectMany(sourceRoot => FindLinePatternViolations(sourceRoot, forbiddenPatterns))
            .Order(StringComparer.Ordinal)];

    public static IEnumerable<string> SourceFiles(string sourceRoot) {
        if (!Directory.Exists(sourceRoot)) {
            return [];
        }

        return Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Order(StringComparer.Ordinal);
    }

    public static IEnumerable<string> SourceFiles(IEnumerable<string> sourceRoots) =>
        sourceRoots
            .SelectMany(SourceFiles)
            .Order(StringComparer.Ordinal);

    public static string[] FindUnsealedConcreteClassDeclarations(
        IEnumerable<string> sourceRoots,
        Func<string, bool>? includePath = null) {
        string repositoryRoot = ArchitectureTestPaths.RepositoryRoot;

        return [.. SourceFiles(sourceRoots)
            .Where(path => includePath?.Invoke(path) ?? true)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => IsUnsealedConcreteClassDeclaration(entry.line))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];
    }

    private static string StripLineComment(string line) {
        int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        return commentIndex < 0 ? line : line[..commentIndex];
    }

    private static bool IsUnsealedConcreteClassDeclaration(string line) {
        string trimmed = line.TrimStart();
        return (trimmed.StartsWith("public class ", StringComparison.Ordinal) ||
                trimmed.StartsWith("internal class ", StringComparison.Ordinal) ||
                trimmed.StartsWith("public partial class ", StringComparison.Ordinal) ||
                trimmed.StartsWith("internal partial class ", StringComparison.Ordinal)) &&
               !trimmed.StartsWith("public static class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("internal static class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("public abstract class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("internal abstract class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("public sealed class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("internal sealed class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("public sealed partial class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("internal sealed partial class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("public static partial class ", StringComparison.Ordinal) &&
               !trimmed.StartsWith("internal static partial class ", StringComparison.Ordinal);
    }
}
