namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationAbstractionsBoundaryTests {
    [Fact]
    public void ApplicationAbstractions_Interfaces_AreKeptInPurposeFolders() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions");
        var allowedPurposeFolders = new HashSet<string>(StringComparer.Ordinal) {
            "Abstractions",
            "Common",
            "Interfaces",
            "Services",
        };

        string[] violations = [.. SourceScanner.SourceFiles(abstractionsRoot)
            .Where(path => Path.GetFileName(path).StartsWith('I'))
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal))
            .Where(path => {
                string relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(abstractionsRoot, path)) ?? string.Empty;
                string[] segments = relativeDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (segments.Length < 2) {
                    return true;
                }

                return string.Equals(segments[0], "Common", StringComparison.Ordinal)
                    ? allowedPurposeFolders.Contains(segments[1]) is false
                    : allowedPurposeFolders.Contains(segments[^1]) is false;
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractions_SourceFiles_DoNotReferenceHostPresentationOrInfrastructureNamespaces() {
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions");
        string[] forbiddenPatterns = [
            "FoodDiary.Web.Api",
            "FoodDiary.Presentation.Api",
            "FoodDiary.Infrastructure",
            "Microsoft.AspNetCore",
            "IActionResult",
            "ControllerBase",
            "HttpContext",
            "IConfiguration",
            "IOptions<",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(abstractionsRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }
}
