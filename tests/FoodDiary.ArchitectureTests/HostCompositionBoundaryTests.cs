using System.Globalization;

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

    [Fact]
    public void PrimaryWebApiHost_RegistersOnlyAllowedHostedServices() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");
        var allowedHostedServiceRegistrations = new HashSet<string>(StringComparer.Ordinal) {
            "InitialAdminHostedService",
        };

        string[] violations = [.. SourceScanner.SourceFiles(hostRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Select(entry => new {
                entry.path,
                entry.index,
                HostedServiceName = TryReadHostedServiceRegistration(entry.line),
            })
            .Where(static entry => entry.HostedServiceName is not null)
            .Where(entry => !allowedHostedServiceRegistrations.Contains(entry.HostedServiceName!))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1} {entry.HostedServiceName}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void TelegramBotProject_ReferencesOnlyTransportAndHostingPackages() {
        var allowedPackages = new HashSet<string>(StringComparer.Ordinal) {
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Http",
            "Telegram.Bot",
        };

        string[] violations = [.. ProjectReferenceReader
            .ReadPackageReferences("FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj")
            .Where(packageName => !allowedPackages.Contains(packageName))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        string.Equals(projectName, "FoodDiary.Mediator", StringComparison.Ordinal)
            ? Path.Combine("Shared", "FoodDiary.Mediator")
            : projectName;

    private static string? TryReadHostedServiceRegistration(string line) {
        const string marker = "AddHostedService<";
        int markerIndex = line.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0) {
            return null;
        }

        int nameStart = markerIndex + marker.Length;
        int nameEnd = line.IndexOf('>', nameStart);
        return nameEnd > nameStart ? line[nameStart..nameEnd] : null;
    }
}
