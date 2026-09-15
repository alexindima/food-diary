namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SharedApplicationContractsBoundaryTests {
    [Fact]
    public void RetiredApplicationAbstractionsAggregator_IsAbsent() {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            ".nuget",
            "lockfiles",
            "FoodDiary.Application.Abstractions.packages.lock.json")));
    }

    [Theory]
    [InlineData("FoodDiary.Audit.Contracts", "FoodDiary.Modules.Users.Domain.Contracts")]
    [InlineData("FoodDiary.Authentication.Contracts")]
    [InlineData("FoodDiary.Email.Contracts")]
    [InlineData("FoodDiary.Nutrition.Contracts")]
    [InlineData("FoodDiary.Outbox.Management.Contracts")]
    public void NarrowSharedContractProjects_HaveOnlyApprovedDependencies(
        string projectName,
        params string[] expectedReferences) {
        string relativeProjectPath = $"Shared/{projectName}/{projectName}.csproj";

        Assert.Equal(expectedReferences.Order(StringComparer.Ordinal),
            ProjectReferenceReader.ReadProjectReferences(relativeProjectPath),
            StringComparer.Ordinal);
        Assert.Empty(ProjectReferenceReader.ReadPackageReferences(relativeProjectPath));
    }

    [Fact]
    public void ApplicationAbstractionsProject_StaysDependencyLightweight() {
        const string relativeProjectPath = "Shared/FoodDiary.Application.Contracts/FoodDiary.Application.Contracts.csproj";

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.Equal(["FoodDiary.Domain.Primitives", "FoodDiary.Mediator", "FoodDiary.Results"], projectReferences);
        Assert.Empty(packageReferences);
    }

    [Fact]
    public void ApplicationAbstractions_SourceFiles_AreKeptOutOfProjectRoot() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Application.Contracts");

        string[] violations = [.. Directory.GetFiles(abstractionsRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractions_FeatureFolders_HavePurposeContractsFolder() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Application.Contracts");
        var excludedDirectories = new HashSet<string>(StringComparer.Ordinal) {
            "bin",
            "Common",
            "obj",
        };

        string[] violations = [.. Directory.GetDirectories(abstractionsRoot)
            .Where(path => !excludedDirectories.Contains(Path.GetFileName(path)))
            .Where(path => SourceScanner.SourceFiles(path).Any())
            .Where(path => !Directory.Exists(Path.Combine(path, "Common")) &&
                           !Directory.Exists(Path.Combine(path, "Abstractions")))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractions_FeatureSourceFiles_StayInPurposeFolders() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Application.Contracts");
        var excludedDirectories = new HashSet<string>(StringComparer.Ordinal) {
            "bin",
            "Common",
            "obj",
        };
        var allowedPurposeFolders = new HashSet<string>(StringComparer.Ordinal) {
            "Abstractions",
            "Common",
            "Models",
            "Services",
        };

        string[] violations = [.. Directory.GetDirectories(abstractionsRoot)
            .Where(path => !excludedDirectories.Contains(Path.GetFileName(path)))
            .SelectMany(featurePath => SourceScanner.SourceFiles(featurePath)
                .Where(sourcePath => {
                    string relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(featurePath, sourcePath)) ?? string.Empty;
                    string firstSegment = relativeDirectory
                        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .FirstOrDefault() ?? string.Empty;

                    return !allowedPurposeFolders.Contains(firstSegment);
                }))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractions_Interfaces_AreKeptInPurposeFolders() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Application.Contracts");
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
                    ? !allowedPurposeFolders.Contains(segments[1])
                    : !allowedPurposeFolders.Contains(segments[^1]);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractions_SourceFiles_DoNotReferenceHostPresentationOrInfrastructureNamespaces() {
        string abstractionsRoot = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Application.Contracts");
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
            "System.Net.Mail",
            "MailMessage",
            "MailAddress",
            "AlternateView",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(abstractionsRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotBuildBclMailTransportMessages() {
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");
        string[] forbiddenPatterns = [
            "System.Net.Mail",
            "MailMessage",
            "MailAddress",
            "AlternateView",
            "MediaTypeNames.Text",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }
}
