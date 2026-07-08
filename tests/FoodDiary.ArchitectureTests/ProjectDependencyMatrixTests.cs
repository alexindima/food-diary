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
                "FoodDiary.Domain.Primitives",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Domain"] = [
                "FoodDiary.Domain.Primitives",
            ],
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
                "FoodDiary.Resources",
            ],
            ["FoodDiary.MailInbox.Application"] = [
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Results",
            ],
            ["FoodDiary.MailInbox.Client"] = [],
            ["FoodDiary.MailInbox.Domain"] = [
                "FoodDiary.Domain.Primitives",
            ],
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
                "FoodDiary.Results",
            ],
            ["FoodDiary.MailRelay.Client"] = [],
            ["FoodDiary.MailRelay.Domain"] = [
                "FoodDiary.Domain.Primitives",
            ],
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
            ["FoodDiary.Domain.Primitives"] = [],
            ["FoodDiary.Resources"] = [
                "FoodDiary.Application.Abstractions",
            ],
            ["FoodDiary.Results"] = [],
            ["FoodDiary.Telegram.Bot"] = [],
            ["FoodDiary.Web.Api"] = [
                "FoodDiary.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Integrations",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Resources",
            ],
        };

    private static readonly IReadOnlyDictionary<string, string[]> AllowedTestProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["FoodDiary.Application.Tests"] = [
                "FoodDiary.Application",
                "FoodDiary.Domain",
            ],
            ["FoodDiary.ArchitectureTests"] = [
                "FoodDiary.Domain",
                "FoodDiary.Infrastructure",
            ],
            ["FoodDiary.Domain.Primitives.Tests"] = [
                "FoodDiary.Domain.Primitives",
            ],
            ["FoodDiary.Domain.Tests"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Infrastructure.IntegrationTests"] = [
                "FoodDiary.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Initializer",
                "FoodDiary.Integrations",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.Infrastructure.Tests"] = [
                "FoodDiary.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Initializer",
                "FoodDiary.Integrations",
            ],
            ["FoodDiary.JobManager.Tests"] = [
                "FoodDiary.JobManager",
            ],
            ["FoodDiary.MailInbox.Application.Tests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
            ],
            ["FoodDiary.MailInbox.Client.Tests"] = [
                "FoodDiary.MailInbox.Client",
            ],
            ["FoodDiary.MailInbox.Domain.Tests"] = [
                "FoodDiary.MailInbox.Domain",
            ],
            ["FoodDiary.MailInbox.Infrastructure.Tests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.MailInbox.Infrastructure",
            ],
            ["FoodDiary.MailInbox.Initializer.Tests"] = [
                "FoodDiary.MailInbox.Initializer",
            ],
            ["FoodDiary.MailInbox.IntegrationTests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.MailInbox.Infrastructure",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.MailInbox.Presentation.Tests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.MailInbox.Presentation",
            ],
            ["FoodDiary.MailRelay.Application.Tests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Domain",
            ],
            ["FoodDiary.MailRelay.Client.Tests"] = [
                "FoodDiary.MailRelay.Client",
            ],
            ["FoodDiary.MailRelay.Domain.Tests"] = [
                "FoodDiary.MailRelay.Domain",
            ],
            ["FoodDiary.MailRelay.Infrastructure.Tests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Client",
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.MailRelay.Infrastructure",
            ],
            ["FoodDiary.MailRelay.Initializer.Tests"] = [
                "FoodDiary.MailRelay.Initializer",
            ],
            ["FoodDiary.MailRelay.IntegrationTests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.MailRelay.Infrastructure",
                "FoodDiary.MailRelay.WebApi",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.MailRelay.Presentation.Tests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Client",
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.MailRelay.Presentation",
            ],
            ["FoodDiary.Mediator.Tests"] = [
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Presentation.Api.Tests"] = [
                "FoodDiary.Application",
                "FoodDiary.Domain",
                "FoodDiary.Presentation.Api",
            ],
            ["FoodDiary.Resources.Tests"] = [
                "FoodDiary.Resources",
            ],
            ["FoodDiary.Results.Tests"] = [
                "FoodDiary.Results",
            ],
            ["FoodDiary.Telegram.Bot.Tests"] = [
                "FoodDiary.Telegram.Bot",
            ],
            ["FoodDiary.Testing"] = [],
            ["FoodDiary.Web.Api.IntegrationTests"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Testing",
                "FoodDiary.Web.Api",
            ],
            ["FoodDiary.Web.Api.Tests"] = [
                "FoodDiary.Integrations",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Web.Api",
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
    public void AllTestProjects_AreCoveredByDependencyMatrix() {
        IReadOnlyList<string> actualProjects = ProjectReferenceReader.ReadTestProjectNames();
        string[] expectedProjects = [.. AllowedTestProjectReferences.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(expectedProjects, actualProjects);
    }

    [Fact]
    public void TestProjectReferences_MatchDependencyMatrix() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadTestProjectReferences();

        foreach ((string? projectName, string[]? expectedReferences) in AllowedTestProjectReferences) {
            Assert.True(
                actualReferencesByProject.TryGetValue(projectName, out string[]? actualReferences),
                $"Test project '{projectName}' is missing from discovered test projects.");

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
        projectName switch {
            "FoodDiary.Mediator" => Path.Combine("Shared", "FoodDiary.Mediator"),
            "FoodDiary.Results" => Path.Combine("Shared", "FoodDiary.Results"),
            "FoodDiary.Domain.Primitives" => Path.Combine("Shared", "FoodDiary.Domain.Primitives"),
            _ => projectName,
        };
}
