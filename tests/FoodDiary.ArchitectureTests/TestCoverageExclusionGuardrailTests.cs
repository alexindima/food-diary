using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TestCoverageExclusionGuardrailTests {
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    [Fact]
    public void TestTypes_AreExcludedFromCodeCoverage() {
        string testRoot = ArchitectureTestPaths.FromRoot("tests");
        string[] sourceFiles = [.. Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))];
        HashSet<string> excludedPartialTypeNames = FindExcludedPartialTypeNames(sourceFiles);
        string[] violations = [.. sourceFiles
            .SelectMany(path => FindTypesWithoutCoverageExclusion(path, excludedPartialTypeNames))
            .OrderBy(static violation => violation.Path, PathComparer)
            .ThenBy(static violation => violation.Line)
            .Select(static violation => violation.Format())];

        Assert.True(
            violations.Length == 0,
            "Test types should be marked with [ExcludeFromCodeCoverage] so test implementation details do not affect dotCover reports. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static HashSet<string> FindExcludedPartialTypeNames(IEnumerable<string> sourceFiles) {
        var excludedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (string path in sourceFiles) {
            Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
            foreach (TypeDeclarationSyntax type in root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Where(static type => type.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)))
                .Where(HasExcludeFromCodeCoverage)) {
                excludedTypeNames.Add(type.Identifier.ValueText);
            }
        }

        return excludedTypeNames;
    }

    private static IEnumerable<CoverageExclusionViolation> FindTypesWithoutCoverageExclusion(
        string path,
        IReadOnlySet<string> excludedPartialTypeNames) {
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
        CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(static type => type is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax)
            .Where(type => !HasExcludeFromCodeCoverage(type) && !IsCoveredPartialDeclaration(type, excludedPartialTypeNames))
            .Select(type => new CoverageExclusionViolation(
                Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path),
                type.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                type.Identifier.ValueText));
    }

    private static bool IsCoveredPartialDeclaration(
        TypeDeclarationSyntax type,
        IReadOnlySet<string> excludedPartialTypeNames) =>
        type.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)) &&
        excludedPartialTypeNames.Contains(type.Identifier.ValueText);

    private static bool HasExcludeFromCodeCoverage(TypeDeclarationSyntax type) =>
        type.AttributeLists
            .SelectMany(static list => list.Attributes)
            .Select(static attribute => attribute.Name.ToString())
            .Any(static name =>
                name.EndsWith("ExcludeFromCodeCoverage", StringComparison.Ordinal) ||
                name.EndsWith("ExcludeFromCodeCoverageAttribute", StringComparison.Ordinal));

    [ExcludeFromCodeCoverage]
    private sealed record CoverageExclusionViolation(string Path, int Line, string TypeName) {
        public string Format() => string.Create(CultureInfo.InvariantCulture, $"{Path}:{Line} {TypeName}");
    }
}
