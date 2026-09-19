using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class SourceScanner {
    public static string[] FindLinePatternViolations(
        string sourceRoot,
        IReadOnlyCollection<string> forbiddenPatterns,
        bool requireSourceRoot = false) {
        if (!Directory.Exists(sourceRoot)) {
            if (requireSourceRoot) {
                throw new DirectoryNotFoundException($"Required source root does not exist: {sourceRoot}");
            }
            return [];
        }

        string repositoryRoot = ArchitectureTestPaths.RepositoryRoot;

        return [.. RepositoryFileDiscovery.EnumerateFiles(sourceRoot, "*.cs")
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .SelectMany(path => ReadCodeLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];
    }

    public static string[] FindLinePatternViolations(
        IEnumerable<string> sourceRoots,
        IReadOnlyCollection<string> forbiddenPatterns,
        bool requireSourceRoot = false) =>
        [.. sourceRoots
            .SelectMany(sourceRoot => FindLinePatternViolations(sourceRoot, forbiddenPatterns, requireSourceRoot))
            .Order(StringComparer.Ordinal)];

    public static IEnumerable<string> SourceFiles(string sourceRoot) {
        if (!Directory.Exists(sourceRoot)) {
            return [];
        }

        return RepositoryFileDiscovery.EnumerateFiles(sourceRoot, "*.cs")
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Order(StringComparer.Ordinal);
    }

    public static IEnumerable<string> SourceFiles(IEnumerable<string> sourceRoots) =>
        sourceRoots
            .SelectMany(SourceFiles)
            .Order(StringComparer.Ordinal);

    internal static string[] ReadCodeLines(string path) {
        string source = File.ReadAllText(path);
        char[] code = source.ToCharArray();
        SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
        IEnumerable<TextSpan> ignoredSpans = root.DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .Select(trivia => trivia.Span)
            .Concat(root.DescendantTokens().Where(token =>
                token.IsKind(SyntaxKind.StringLiteralToken) ||
                token.IsKind(SyntaxKind.Utf8StringLiteralToken) ||
                token.IsKind(SyntaxKind.InterpolatedStringTextToken) ||
                token.IsKind(SyntaxKind.CharacterLiteralToken)).Select(token => token.Span));
        foreach (TextSpan span in ignoredSpans) {
            for (int index = span.Start; index < span.End; index++) {
                if (code[index] is not '\r' and not '\n') { code[index] = ' '; }
            }
        }
        return new string(code).Split(["\r\n", "\n"], StringSplitOptions.None);
    }

}
