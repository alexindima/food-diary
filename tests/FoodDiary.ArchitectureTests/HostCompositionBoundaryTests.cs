namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HostCompositionBoundaryTests {
    [Fact]
    public void NonHostProductionProjects_DoNotReferencePrimaryWebApiNamespace() {
        string[] nonHostRoots = [.. ProjectReferenceReader.ReadProductionProjectNames()
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.Web.Api", StringComparison.Ordinal))
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.Initializer", StringComparison.Ordinal))
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.JobManager", StringComparison.Ordinal))
            .Where(static projectName => !projectName.EndsWith(".WebApi", StringComparison.Ordinal))
            .Where(static projectName => !projectName.EndsWith(".Initializer", StringComparison.Ordinal))
            .Select(projectName => ArchitectureTestPaths.FromRoot(ProjectFolderFromProjectName(projectName)))];

        string[] violations = SourceScanner.FindLinePatternViolations(nonHostRoots, ["FoodDiary.Web.Api"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAndPresentationProjects_DoNotReferenceHostOnlyOptionsOrExtensions() {
        string[] sourceRoots = [
            ArchitectureTestPaths.FromRoot("FoodDiary.Application"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Presentation.Api"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Resources"),
        ];
        string[] forbiddenPatterns = [
            "FoodDiary.Web.Api.Options",
            "FoodDiary.Web.Api.Extensions",
            "ApiServiceCollectionExtensions",
            "ApiApplicationBuilderExtensions",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(sourceRoots, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void PrimaryWebApiHost_DoesNotUseDomainTypesDirectly() {
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");

        string[] violations = SourceScanner.FindLinePatternViolations(
            hostRoot,
            ["using FoodDiary.Domain", "FoodDiary.Domain."]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PrimaryWebApiHost_UsesTimeProviderInsteadOfDateTimeUtcNow() {
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");

        string[] violations = SourceScanner.FindLinePatternViolations(hostRoot, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApi_UsesTimeProviderInsteadOfDirectUtcNow() {
        string presentationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Presentation.Api");

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.Empty(violations);
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        string.Equals(projectName, "FoodDiary.Mediator", StringComparison.Ordinal)
            ? Path.Combine("Shared", "FoodDiary.Mediator")
            : projectName;
}
