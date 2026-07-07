using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class LayeringTests {
    [Fact]
    public void DomainProject_DoesNotReference_OtherApplicationLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Domain/FoodDiary.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Abstractions", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void ApplicationAbstractionsProject_ReferencesOnly_DomainAmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj");

        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
    }

    [Fact]
    public void ApplicationProject_ReferencesOnly_DomainAndContracts_AmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void InfrastructureProject_ReferencesOnly_DomainAndApplicationAbstractions_AmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
    }

    [Fact]
    public void IntegrationsProject_ReferencesApplicationAbstractionsAndExternalClients_ButNotInfrastructure() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Integrations/FoodDiary.Integrations.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.Contains("FoodDiary.Domain", references);
        Assert.Contains("FoodDiary.MailInbox.Client", references);
        Assert.Contains("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void IntegrationsProject_PackageReferencesStayLimitedToApprovedProvidersAndTransport() {
        string[] allowedPackages = [
            "AWSSDK.S3",
            "Microsoft.AspNetCore.WebUtilities",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Http.Resilience",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Microsoft.IdentityModel.Protocols.OpenIdConnect",
            "Stripe.net",
            "System.IdentityModel.Tokens.Jwt",
            "WebPush",
        ];

        string[] packages = ProjectReferenceReader.ReadPackageReferences("FoodDiary.Integrations/FoodDiary.Integrations.csproj");

        Assert.Equal(allowedPackages, packages);
    }

    [Fact]
    public void IntegrationsProject_UsesTimeProviderInsteadOfDirectUtcNow() {
        string integrationsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Integrations");

        string[] violations = SourceScanner.FindLinePatternViolations(integrationsRoot, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void IntegrationsSource_DoesNotReferenceConcreteApplicationHostPresentationOrServerMailLayers() {
        string integrationsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Integrations");
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] forbiddenPatterns = [
            "FoodDiary.Application;",
            "using FoodDiary.Infrastructure",
            "FoodDiary.Infrastructure.",
            "using FoodDiary.Presentation.Api",
            "FoodDiary.Presentation.Api.",
            "using FoodDiary.Web.Api",
            "FoodDiary.Web.Api.",
            "using FoodDiary.Resources",
            "FoodDiary.Resources.",
            "FoodDiary.MailInbox.Application",
            "FoodDiary.MailInbox.Domain",
            "FoodDiary.MailInbox.Infrastructure",
            "FoodDiary.MailInbox.Presentation",
            "FoodDiary.MailInbox.WebApi",
            "FoodDiary.MailRelay.Application",
            "FoodDiary.MailRelay.Domain",
            "FoodDiary.MailRelay.Infrastructure",
            "FoodDiary.MailRelay.Presentation",
            "FoodDiary.MailRelay.WebApi",
            "ControllerBase",
            "IActionResult",
            "HttpContext",
            "DbContext",
            "Npgsql",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(integrationsRoot)
            .Where(static path => !path.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void IntegrationsRootFolders_StayLimitedToProviderAdapterAreas() {
        string integrationsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Integrations");
        string[] allowedDirectories = [
            "Authentication",
            "Billing",
            "Options",
            "Properties",
            "Services",
            "Wearables",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(integrationsRoot)
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
    public void IntegrationsOptions_AreKeptInOptionsFolder() {
        string integrationsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Integrations");
        string optionsRoot = Path.Combine(integrationsRoot, "Options");
        var optionsTypePattern = new Regex(
            @"\b(?:class|record)\s+\w+Options\b",
            RegexOptions.Compiled,
            TimeSpan.FromSeconds(1));

        string[] violations = [.. SourceScanner.SourceFiles(integrationsRoot)
            .Where(path => !path.StartsWith(optionsRoot, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => optionsTypePattern.IsMatch(entry.line))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void IntegrationsCompositionRoot_StaysLimitedToApprovedProviderModules() {
        string dependencyInjectionPath = ArchitectureTestPaths.FromRoot("FoodDiary.Integrations", "DependencyInjection.cs");
        string[] expectedRegistrations = [
            "services.AddIntegrationOptions(configuration);",
            "services.AddStorageIntegrations();",
            "services.AddMailIntegrations(configuration);",
            "services.AddAuthenticationIntegrations();",
            "services.AddBillingIntegrations();",
            "services.AddNotificationIntegrations();",
            "services.AddAiIntegrations();",
            "services.AddFoodDataIntegrations(configuration);",
            "services.AddWearableIntegrations();",
        ];

        string[] actualRegistrations = [.. File.ReadLines(dependencyInjectionPath)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("services.", StringComparison.Ordinal))];

        Assert.Equal(expectedRegistrations, actualRegistrations);
    }

    [Fact]
    public void InfrastructureProject_PackageReferencesStayLimitedToPersistenceAndTechnicalImplementations() {
        string[] allowedPackages = [
            "BCrypt.Net-Next",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.CSharp.Workspaces",
            "Microsoft.CodeAnalysis.Common",
            "Microsoft.CodeAnalysis.Workspaces.MSBuild",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Design",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Json",
            "Microsoft.Extensions.Configuration.UserSecrets",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Newtonsoft.Json",
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "QuestPDF",
            "SkiaSharp",
            "SkiaSharp.NativeAssets.Linux.NoDependencies",
            "System.IdentityModel.Tokens.Jwt",
        ];

        string[] packages = ProjectReferenceReader.ReadPackageReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.Equal(allowedPackages, packages);
    }

    [Fact]
    public void InfrastructureRootFolders_StayLimitedToTechnicalImplementationAreas() {
        string infrastructureRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure");
        string[] allowedDirectories = [
            "Authentication",
            "Events",
            "Migrations",
            "Options",
            "Persistence",
            "Properties",
            "Services",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(infrastructureRoot)
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
    public void InfrastructureCompositionRoot_StaysLimitedToApprovedTechnicalModules() {
        string dependencyInjectionPath = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "DependencyInjection.cs");
        string[] expectedRegistrations = [
            "services.TryAddSingleton(TimeProvider.System);",
            "services.AddMemoryCache();",
            "services.AddLogging();",
            "services.AddInfrastructureOptions(configuration);",
            "services.AddPersistence(configuration);",
            "services.AddFeatureRepositories();",
            "services.AddAuthenticationInfrastructure();",
            "services.AddBillingInfrastructure();",
            "services.AddExportInfrastructure();",
            "services.AddWearablesInfrastructure();",
        ];

        string[] actualRegistrations = [.. File.ReadLines(dependencyInjectionPath)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("services.", StringComparison.Ordinal))];

        Assert.Equal(expectedRegistrations, actualRegistrations);
    }

    [Fact]
    public void InfrastructureNarrowRepositoryFacades_AreRegisteredThroughFullRepositoryService() {
        string infrastructureRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure");
        string[] narrowRepositoryMarkers = [
            "AdminReadRepository,",
            "LookupRepository,",
            "ReadRepository,",
            "WriteRepository,",
        ];

        string[] violations = [.. Directory.GetFiles(infrastructureRoot, "DependencyInjection*.cs", SearchOption.TopDirectoryOnly)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = line.Trim() }))
            .Where(entry => entry.line.StartsWith("services.AddScoped<I", StringComparison.Ordinal))
            .Where(entry => narrowRepositoryMarkers.Any(marker => entry.line.Contains(marker, StringComparison.Ordinal)))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructureFeatureRepositoryComposition_StaysLimitedToApprovedFeatureModules() {
        string dependencyInjectionPath = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "DependencyInjection.Repositories.cs");
        string[] expectedRegistrations = [
            "services.AddUserPersistence();",
            "services.AddFoodPersistence();",
            "services.AddDashboardReadServices();",
            "services.AddShoppingListPersistence();",
            "services.AddTrackingPersistence();",
            "services.AddImagePersistence();",
            "services.AddAiPersistence();",
            "services.AddDietologistPersistence();",
            "services.AddNotificationPersistence();",
            "services.AddProviderCachePersistence();",
            "services.AddFastingPersistence();",
            "services.AddFavoritesPersistence();",
            "services.AddLearningPersistence();",
            "services.AddRecipeInteractionPersistence();",
            "services.AddModerationPersistence();",
            "services.AddUsdaPersistence();",
        ];

        string[] actualRegistrations = [.. File.ReadLines(dependencyInjectionPath)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("services.", StringComparison.Ordinal))];

        Assert.Equal(expectedRegistrations, actualRegistrations);
    }

    [Fact]
    public void InfrastructureConcreteClasses_AreSealedOrStatic() {
        string root = GetRepositoryRoot();
        string infrastructureRoot = Path.Combine(root, "FoodDiary.Infrastructure");

        string[] violations = SourceScanner.FindUnsealedConcreteClassDeclarations(
            [infrastructureRoot],
            static path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApiProject_ReferencesApplication_ButNotInfrastructure() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj");

        Assert.Contains("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
    }

    [Fact]
    public void ResourcesProject_ReferencesOnly_ApplicationAmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Resources/FoodDiary.Resources.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
    }

    [Fact]
    public void ResourcesSource_DoesNotReferenceConcreteBackendOrTransportLayers() {
        string resourcesRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Resources");

        string[] violations = SourceScanner.FindLinePatternViolations(resourcesRoot, [
            "FoodDiary.Application;",
            "FoodDiary.Domain",
            "FoodDiary.Infrastructure",
            "FoodDiary.Presentation.Api",
            "FoodDiary.Web.Api",
            "Microsoft.AspNetCore",
            "ControllerBase",
            "IActionResult",
            "HttpContext",
            "DbContext",
            "Npgsql",
            "IConfiguration",
            "IOptions<",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseDomainNamespaces() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, [
            "using FoodDiary.Domain",
            "FoodDiary.Domain.",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void WebApiProject_IsHostAndReferencesPresentationApplicationAndInfrastructure() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Web.Api/FoodDiary.Web.Api.csproj");

        Assert.Contains("FoodDiary.Application", references);
        Assert.Contains("FoodDiary.Infrastructure", references);
        Assert.Contains("FoodDiary.Integrations", references);
        Assert.Contains("FoodDiary.Presentation.Api", references);
        Assert.Contains("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
    }

    [Fact]
    public void JobManagerProject_DoesNotReference_WebApi() {
        HashSet<string> references = GetProjectReferences("FoodDiary.JobManager/FoodDiary.JobManager.csproj");

        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void TelegramBotProject_DoesNotReference_CoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj");

        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void PresentationApi_OnlyBaseControllersRemainInControllersFolder() {
        string root = GetRepositoryRoot();
        string[] controllerFiles = Directory.GetFiles(Path.Combine(root, "FoodDiary.Presentation.Api", "Controllers"), "*Controller.cs");
        string?[] names = [.. controllerFiles.Select(Path.GetFileNameWithoutExtension)];

        Assert.Contains("BaseApiController", names);
        Assert.Contains("AuthorizedController", names);
        Assert.Equal(2, names.Length);
    }

    [Fact]
    public void PresentationApi_EndpointControllersLiveUnderFeatures() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");
        string controllersRoot = Path.Combine(presentationRoot, "Controllers");

        string[] violations = [.. Directory.GetFiles(presentationRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(controllersRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static HashSet<string> GetProjectReferences(string relativeProjectPath) {
        string root = GetRepositoryRoot();
        string projectPath = Path.Combine(root, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => GetProjectNameFromReference(value!))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string GetRepositoryRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            string solutionPath = Path.Combine(current.FullName, "FoodDiary.slnx");
            if (File.Exists(solutionPath)) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private static string GetProjectNameFromReference(string includeValue) {
        string normalized = includeValue.Replace('\\', '/');
        string fileName = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Path.GetFileNameWithoutExtension(fileName);
    }
}
