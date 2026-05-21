namespace FoodDiary.ArchitectureTests;

public sealed class ApplicationAbstractionsBoundaryTests {
    [Fact]
    public void ApplicationAbstractions_Interfaces_AreKeptInPurposeFolders() {
        var root = ArchitectureTestPaths.RepositoryRoot;
        var abstractionsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions");
        var allowedPurposeFolders = new HashSet<string>(StringComparer.Ordinal) {
            "Abstractions",
            "Common",
            "Interfaces",
            "Services",
        };

        var violations = SourceScanner.SourceFiles(abstractionsRoot)
            .Where(path => Path.GetFileName(path).StartsWith("I", StringComparison.Ordinal))
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal))
            .Where(path => {
                var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(abstractionsRoot, path)) ?? string.Empty;
                var segments = relativeDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (segments.Length < 2) {
                    return true;
                }

                return string.Equals(segments[0], "Common", StringComparison.Ordinal)
                    ? allowedPurposeFolders.Contains(segments[1]) is false
                    : allowedPurposeFolders.Contains(segments[^1]) is false;
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractions_SourceFiles_DoNotReferenceHostPresentationOrInfrastructureNamespaces() {
        var abstractionsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions");
        var forbiddenPatterns = new[] {
            "FoodDiary.Web.Api",
            "FoodDiary.Presentation.Api",
            "FoodDiary.Infrastructure",
            "Microsoft.AspNetCore",
            "IActionResult",
            "ControllerBase",
            "HttpContext",
            "IConfiguration",
            "IOptions<",
        };

        var violations = SourceScanner.FindLinePatternViolations(abstractionsRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }
}
