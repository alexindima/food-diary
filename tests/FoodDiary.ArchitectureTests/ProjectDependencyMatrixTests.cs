namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProjectDependencyMatrixTests {
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProductionProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["FoodDiary.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Abstractions"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Domain"] = [],
            ["FoodDiary.Infrastructure"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Initializer"] = [
                "FoodDiary.Application",
                "FoodDiary.Infrastructure",
            ],
            ["FoodDiary.Integrations"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.MailInbox.Client",
                "FoodDiary.MailRelay.Client",
            ],
            ["FoodDiary.JobManager"] = [
                "FoodDiary.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Integrations",
            ],
            ["FoodDiary.MailInbox.Application"] = [
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.MailInbox.Client"] = [],
            ["FoodDiary.MailInbox.Domain"] = [],
            ["FoodDiary.MailInbox.Infrastructure"] = [
                "FoodDiary.MailInbox.Application",
            ],
            ["FoodDiary.MailInbox.Initializer"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Infrastructure",
            ],
            ["FoodDiary.MailInbox.Presentation"] = [
                "FoodDiary.MailInbox.Application",
            ],
            ["FoodDiary.MailInbox.WebApi"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Infrastructure",
                "FoodDiary.MailInbox.Presentation",
            ],
            ["FoodDiary.MailRelay.Application"] = [
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.MailRelay.Client"] = [],
            ["FoodDiary.MailRelay.Domain"] = [],
            ["FoodDiary.MailRelay.Infrastructure"] = [
                "FoodDiary.MailRelay.Application",
            ],
            ["FoodDiary.MailRelay.Initializer"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Infrastructure",
            ],
            ["FoodDiary.MailRelay.Presentation"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Client",
            ],
            ["FoodDiary.MailRelay.WebApi"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Infrastructure",
                "FoodDiary.MailRelay.Presentation",
            ],
            ["FoodDiary.Mediator"] = [],
            ["FoodDiary.Presentation.Api"] = [
                "FoodDiary.Application",
            ],
            ["FoodDiary.Resources"] = [
                "FoodDiary.Application.Abstractions",
            ],
            ["FoodDiary.Telegram.Bot"] = [],
            ["FoodDiary.Web.Api"] = [
                "FoodDiary.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Integrations",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Resources",
            ],
        };

    [Fact]
    public void AllProductionProjects_AreCoveredByDependencyMatrix() {
        IReadOnlyList<string> actualProjects = ProjectReferenceReader.ReadProductionProjectNames();
        string[] expectedProjects = [.. AllowedProductionProjectReferences.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(expectedProjects, actualProjects);
    }

    [Fact]
    public void ProductionProjectReferences_MatchDependencyMatrix() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();

        foreach ((string? projectName, string[]? expectedReferences) in AllowedProductionProjectReferences) {
            Assert.True(
                actualReferencesByProject.TryGetValue(projectName, out string[]? actualReferences),
                $"Project '{projectName}' is missing from discovered production projects.");

            Assert.Equal(
                expectedReferences.Order(StringComparer.Ordinal).ToArray(),
                actualReferences);
        }
    }

    [Fact]
    public void CoreProjects_ReferenceMailBoundedContextsOnlyThroughAllowedClientProjects() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();
        string[] coreProjects = [.. actualReferencesByProject.Keys
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal))
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))];

        var allowedMailClientReferences = new HashSet<string>(StringComparer.Ordinal) {
            "FoodDiary.MailInbox.Client",
            "FoodDiary.MailRelay.Client",
        };

        string[] violations = [.. coreProjects
            .SelectMany(projectName => actualReferencesByProject[projectName]
                .Where(static reference => reference.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal) ||
                                           reference.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
                .Where(reference => !allowedMailClientReferences.Contains(reference) ||
                                    !string.Equals(projectName, "FoodDiary.Integrations", StringComparison.Ordinal))
                .Select(reference => $"{projectName} -> {reference}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void CoreProjectSource_ReferencesMailBoundedContextsOnlyFromIntegrations() {
        string[] coreSourceRoots = [.. ProjectReferenceReader.ReadProductionProjectNames()
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal))
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.Integrations", StringComparison.Ordinal))
            .Select(projectName => ArchitectureTestPaths.FromRoot(ProjectFolderFromProjectName(projectName)))];

        string[] violations = SourceScanner.FindLinePatternViolations(coreSourceRoots, [
            "FoodDiary.MailInbox",
            "FoodDiary.MailRelay",
        ]);

        Assert.Empty(violations);
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        string.Equals(projectName, "FoodDiary.Mediator", StringComparison.Ordinal)
            ? Path.Combine("Shared", "FoodDiary.Mediator")
            : projectName;
}
