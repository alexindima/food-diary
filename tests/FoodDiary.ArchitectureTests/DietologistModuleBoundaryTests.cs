using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DietologistModuleBoundaryTests {
    [Fact]
    public void Dietologist_DoesNotDependOnAuthenticationOrNotificationImplementationNamespaces() {
        string root = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Dietologist");
        string[] forbiddenPrefixes = [
            "FoodDiary.Application.Authentication",
            "FoodDiary.Application.Notifications",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(root)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(usingDirective => new {
                    Path = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path),
                    Namespace = usingDirective.Name?.ToString() ?? string.Empty,
                }))
            .Where(item => forbiddenPrefixes.Any(prefix => item.Namespace.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(item => $"{item.Path}: {item.Namespace}")];

        Assert.Empty(violations);
    }

    [Fact]
    public void Dietologist_DoesNotDependOnOtherApplicationFeatures() {
        string root = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Dietologist");
        string[] allowedPrefixes = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Dietologist",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(root)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(usingDirective => new {
                    Path = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path),
                    Namespace = usingDirective.Name?.ToString() ?? string.Empty,
                }))
            .Where(item => item.Namespace.StartsWith("FoodDiary.Application.", StringComparison.Ordinal))
            .Where(item => !allowedPrefixes.Any(prefix => item.Namespace.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(item => $"{item.Path}: {item.Namespace}")];

        Assert.Empty(violations);
    }
}
