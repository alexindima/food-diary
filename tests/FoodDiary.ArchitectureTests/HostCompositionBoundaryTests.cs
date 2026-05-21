namespace FoodDiary.ArchitectureTests;

public sealed class HostCompositionBoundaryTests {
    [Fact]
    public void NonHostProductionProjects_DoNotReferencePrimaryWebApiNamespace() {
        var nonHostRoots = ProjectReferenceReader.ReadProductionProjectNames()
            .Where(static projectName => string.Equals(projectName, "FoodDiary.Web.Api", StringComparison.Ordinal) is false)
            .Where(static projectName => string.Equals(projectName, "FoodDiary.Initializer", StringComparison.Ordinal) is false)
            .Where(static projectName => string.Equals(projectName, "FoodDiary.JobManager", StringComparison.Ordinal) is false)
            .Where(static projectName => projectName.EndsWith(".WebApi", StringComparison.Ordinal) is false)
            .Where(static projectName => projectName.EndsWith(".Initializer", StringComparison.Ordinal) is false)
            .Select(projectName => ArchitectureTestPaths.FromRoot(ProjectFolderFromProjectName(projectName)))
            .ToArray();

        var violations = SourceScanner.FindLinePatternViolations(nonHostRoots, ["FoodDiary.Web.Api"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAndPresentationProjects_DoNotReferenceHostOnlyOptionsOrExtensions() {
        var sourceRoots = new[] {
            ArchitectureTestPaths.FromRoot("FoodDiary.Application"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Presentation.Api"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Resources"),
        };
        var forbiddenPatterns = new[] {
            "FoodDiary.Web.Api.Options",
            "FoodDiary.Web.Api.Extensions",
            "ApiServiceCollectionExtensions",
            "ApiApplicationBuilderExtensions",
        };

        var violations = SourceScanner.FindLinePatternViolations(sourceRoots, forbiddenPatterns);

        Assert.Empty(violations);
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        string.Equals(projectName, "FoodDiary.Mediator", StringComparison.Ordinal)
            ? Path.Combine("Shared", "FoodDiary.Mediator")
            : projectName;
}
