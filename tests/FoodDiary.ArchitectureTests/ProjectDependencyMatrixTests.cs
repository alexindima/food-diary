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
        var actualProjects = ProjectReferenceReader.ReadProductionProjectNames();
        var expectedProjects = AllowedProductionProjectReferences.Keys
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedProjects, actualProjects);
    }

    [Fact]
    public void ProductionProjectReferences_MatchDependencyMatrix() {
        var actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();

        foreach (var (projectName, expectedReferences) in AllowedProductionProjectReferences) {
            Assert.True(
                actualReferencesByProject.TryGetValue(projectName, out var actualReferences),
                $"Project '{projectName}' is missing from discovered production projects.");

            Assert.Equal(
                expectedReferences.OrderBy(static name => name, StringComparer.Ordinal).ToArray(),
                actualReferences);
        }
    }

    [Fact]
    public void CoreProjects_ReferenceMailBoundedContextsOnlyThroughAllowedClientProjects() {
        var actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();
        var coreProjects = actualReferencesByProject.Keys
            .Where(static projectName => projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal) is false)
            .Where(static projectName => projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal) is false)
            .ToArray();

        var allowedMailClientReferences = new HashSet<string>(StringComparer.Ordinal) {
            "FoodDiary.MailInbox.Client",
            "FoodDiary.MailRelay.Client",
        };

        var violations = coreProjects
            .SelectMany(projectName => actualReferencesByProject[projectName]
                .Where(static reference => reference.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal) ||
                                           reference.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
                .Where(reference => allowedMailClientReferences.Contains(reference) is false ||
                                    string.Equals(projectName, "FoodDiary.Integrations", StringComparison.Ordinal) is false)
                .Select(reference => $"{projectName} -> {reference}"))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }
}
