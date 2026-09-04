using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DietologistModuleBoundaryTests {
    [Fact]
    public void DietologistApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Dietologist");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Dietologist", "Application");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Modules.Dietologist.Application.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedDietologistAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Modules.Dietologist.Application", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedDietologistAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Dietologist/Application/FoodDiary.Modules.Dietologist.Application.csproj");
        string[] expectedReferences = ["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterDietologistModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddDietologistModule()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Dietologist_DoesNotDependOnAuthenticationOrNotificationImplementationNamespaces() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Dietologist", "Application");
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
        string root = ArchitectureTestPaths.FromRoot("Modules", "Dietologist", "Application");
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
