using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HostCompositionBoundaryTests {
    [Fact]
    public void PrimaryWebApiHost_ProjectGuide_Exists() {
        string guidePath = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api", "AGENTS.md");

        Assert.True(File.Exists(guidePath), $"Expected project guide at '{guidePath}'.");
    }

    [Fact]
    public void PrimaryWebApiHost_RootFoldersStayLimitedToCompositionStructure() {
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");
        string[] allowedDirectories = [
            "Build",
            "Extensions",
            "HealthChecks",
            "Options",
            "Properties",
            "Services",
            "Swagger",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(hostRoot)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .Where(name => !name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            .Where(name => !name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            .Where(name => !allowedDirectories.Contains(name, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unexpectedDirectories);
    }

    [Fact]
    public void PrimaryWebApiHost_SourceFiles_AreKeptOutOfProjectRootExceptProgram() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");
        string[] allowedRootFiles = [
            "Program.cs",
        ];

        string[] unexpectedRootFiles = [.. Directory.GetFiles(hostRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => !allowedRootFiles.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unexpectedRootFiles);
    }

    [Fact]
    public void PrimaryWebApiHost_DoesNotDeclareFeatureControllersOrTransportModels() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");
        string[] forbiddenDeclarationPatterns = [
            " class ",
            " record ",
            " record class ",
            " record struct ",
        ];
        string[] forbiddenTypeNameMarkers = [
            "Controller",
            "HttpRequest",
            "HttpQuery",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(hostRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = line.Trim() }))
            .Where(entry => forbiddenDeclarationPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Where(entry => forbiddenTypeNameMarkers.Any(marker => entry.line.Contains(marker, StringComparison.Ordinal)))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PrimaryWebApiHost_DoesNotUseMvcControllerSurface() {
        string hostRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api");

        string[] violations = SourceScanner.FindLinePatternViolations(hostRoot, [
            "[ApiController]",
            "ControllerBase",
            "IActionResult",
            "ActionResult<",
        ]);

        Assert.Empty(violations);
    }

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
    public void WebApiHosts_DoNotMapApiFeatureEndpointsDirectly() {
        string[] hostRoots = [
            ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api"),
            ArchitectureTestPaths.FromRoot("MailInbox/FoodDiary.MailInbox.WebApi"),
            ArchitectureTestPaths.FromRoot("MailRelay/FoodDiary.MailRelay.WebApi"),
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(hostRoots, [
            "MapGet(\"/api",
            "MapPost(\"/api",
            "MapPut(\"/api",
            "MapPatch(\"/api",
            "MapDelete(\"/api",
        ]);

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
    public void HostEntryPoints_DoNotUseMediatorDirectlyExceptInitialAdminBootstrap() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] hostRoots = [
            ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api"),
            ArchitectureTestPaths.FromRoot("MailInbox/FoodDiary.MailInbox.WebApi"),
            ArchitectureTestPaths.FromRoot("MailRelay/FoodDiary.MailRelay.WebApi"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Initializer"),
            ArchitectureTestPaths.FromRoot("MailInbox/FoodDiary.MailInbox.Initializer"),
            ArchitectureTestPaths.FromRoot("MailRelay/FoodDiary.MailRelay.Initializer"),
        ];
        string allowedPath = Path.Combine(
            ArchitectureTestPaths.FromRoot("FoodDiary.Web.Api"),
            "Services",
            "InitialAdminHostedService.cs");

        string[] violations = [.. SourceScanner.SourceFiles(hostRoots)
            .Where(path => !string.Equals(path, allowedPath, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry =>
                entry.line.Contains("ISender", StringComparison.Ordinal) ||
                entry.line.Contains("IMediator", StringComparison.Ordinal) ||
                entry.line.Contains(".Send(", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InitializerSource_DoesNotReferenceHttpPresentationOrMediatorSurface() {
        string[] initializerRoots = [
            ArchitectureTestPaths.FromRoot("FoodDiary.Initializer"),
            ArchitectureTestPaths.FromRoot("MailInbox/FoodDiary.MailInbox.Initializer"),
            ArchitectureTestPaths.FromRoot("MailRelay/FoodDiary.MailRelay.Initializer"),
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(initializerRoots, [
            "FoodDiary.Presentation.Api",
            "Microsoft.AspNetCore.Mvc",
            "ControllerBase",
            "IActionResult",
            "HttpContext",
            "MapGet(",
            "MapPost(",
            "MapPut(",
            "MapPatch(",
            "MapDelete(",
            "ISender",
            "IMediator",
            ".Send(",
        ]);

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

    [Fact]
    public void TelegramBotSource_DoesNotReferenceCoreBackendNamespaces() {
        string botRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Telegram.Bot");
        string[] forbiddenPatterns = [
            "FoodDiary.Domain",
            "FoodDiary.Application",
            "FoodDiary.Infrastructure",
            "FoodDiary.Resources",
            "FoodDiary.Presentation.Api",
            "FoodDiary.Web.Api",
            "ControllerBase",
            "IActionResult",
            "DbContext",
            "Npgsql",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(botRoot, forbiddenPatterns);

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
