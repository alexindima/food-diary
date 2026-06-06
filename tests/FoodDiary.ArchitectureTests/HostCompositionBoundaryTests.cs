namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HostCompositionBoundaryTests {
    [Fact]
    public void NonHostProductionProjects_DoNotReferencePrimaryWebApiNamespace() {
        string[] nonHostRoots = ProjectReferenceReader.ReadProductionProjectNames()
            .Where(static projectName => string.Equals(projectName, "FoodDiary.Web.Api", StringComparison.Ordinal) is false)
            .Where(static projectName => string.Equals(projectName, "FoodDiary.Initializer", StringComparison.Ordinal) is false)
            .Where(static projectName => string.Equals(projectName, "FoodDiary.JobManager", StringComparison.Ordinal) is false)
            .Where(static projectName => projectName.EndsWith(".WebApi", StringComparison.Ordinal) is false)
            .Where(static projectName => projectName.EndsWith(".Initializer", StringComparison.Ordinal) is false)
            .Select(projectName => ArchitectureTestPaths.FromRoot(ProjectFolderFromProjectName(projectName)))
            .ToArray();

        string[] violations = SourceScanner.FindLinePatternViolations(nonHostRoots, ["FoodDiary.Web.Api"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAndPresentationProjects_DoNotReferenceHostOnlyOptionsOrExtensions() {
        string[] sourceRoots = new[] {
            ArchitectureTestPaths.FromRoot("FoodDiary.Application"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Presentation.Api"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Resources"),
        };
        string[] forbiddenPatterns = new[] {
            "FoodDiary.Web.Api.Options",
            "FoodDiary.Web.Api.Extensions",
            "ApiServiceCollectionExtensions",
            "ApiApplicationBuilderExtensions",
        };

        string[] violations = SourceScanner.FindLinePatternViolations(sourceRoots, forbiddenPatterns);

        Assert.Empty(violations);
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        string.Equals(projectName, "FoodDiary.Mediator", StringComparison.Ordinal)
            ? Path.Combine("Shared", "FoodDiary.Mediator")
            : projectName;
}
