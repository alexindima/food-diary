using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayArchitectureTests {
    [Fact]
    public void MailRelayDomainProject_DoesNotReferenceOtherMailRelayLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.Domain/FoodDiary.MailRelay.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.MailRelay.Application", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayApplicationProject_ReferencesDomainOnlyAmongMailRelayLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.Application/FoodDiary.MailRelay.Application.csproj");

        Assert.Contains("FoodDiary.MailRelay.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayClientProject_DoesNotReferenceMailRelayLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.Client/FoodDiary.MailRelay.Client.csproj");

        Assert.DoesNotContain("FoodDiary.MailRelay.Application", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayInfrastructureProject_ReferencesApplicationButNotPresentationOrWebApi() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.Infrastructure/FoodDiary.MailRelay.Infrastructure.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayInitializerProject_ReferencesApplicationAndInfrastructureOnlyAmongMailRelayLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.Contains("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayPresentationProject_ReferencesApplicationAndClientButNotInfrastructureOrWebApi() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.Presentation/FoodDiary.MailRelay.Presentation.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.Contains("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayWebApiProject_IsHostAndReferencesApplicationInfrastructureAndPresentation() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.Contains("FoodDiary.MailRelay.Infrastructure", references);
        Assert.Contains("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Domain", references);
    }

    [Fact]
    public void MailRelayDomainSource_DoesNotReferenceFrameworkOrInfrastructureTypes() {
        string root = GetRepositoryRoot();
        string domainRoot = Path.Combine(root, "FoodDiary.MailRelay.Domain");
        string[] forbiddenPatterns = [
            "Microsoft.",
            "Npgsql",
            "RabbitMQ",
            "MailKit",
            "MimeKit",
            "DnsClient",
            "IOptions",
            "IConfiguration",
            "HttpContext",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(domainRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayApplicationSource_DoesNotReferenceTransportPersistenceOrConfigurationTypes() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.MailRelay.Application");
        string[] forbiddenPatterns = [
            "Microsoft.AspNetCore",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Configuration",
            "IOptions<",
            "IConfiguration",
            "Npgsql",
            "RabbitMQ",
            "MailKit",
            "MimeKit",
            "DnsClient",
            "HttpContext",
            "HttpRequest",
            "IEndpointRouteBuilder",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationSource_DoesNotReferenceInfrastructureLayer() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        string[] forbiddenPatterns = [
            "FoodDiary.MailRelay.Infrastructure",
            "MailRelayQueueStore",
            "Npgsql",
            "RabbitMQ",
            "MailKit",
            "DnsClient",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationControllers_UseMediatorInsteadOfApplicationServicesDirectly() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        string[] forbiddenPatterns = [
            "MailRelayEmailUseCases",
            "MailRelayDeliveryEventIngestionService",
            "IMailRelayQueueStore",
            "IMailRelayDispatchNotifier",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayEmailControllers_RequireRelayApiKeyThroughAuthorizedBaseController() {
        string root = GetRepositoryRoot();
        string emailFeatureRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation", "Features", "Email");

        string[] violations = [.. Directory.GetFiles(emailFeatureRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .Where(path => File.ReadAllText(path).Contains(": AuthorizedMailRelayController", StringComparison.Ordinal) is false)
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayWebApiProgram_UsesMvcControllersForEmailEndpoints() {
        string root = GetRepositoryRoot();
        string programPath = Path.Combine(root, "FoodDiary.MailRelay.WebApi", "Program.cs");
        string content = File.ReadAllText(programPath);

        Assert.DoesNotContain("MapGet", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", content, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/email", content, StringComparison.Ordinal);
        Assert.Contains("MapMailRelayPresentation", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapMailRelayEndpoints", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MailRelayPresentationControllers_AreKeptInFeatureFolders() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        var allowedControllerFiles = new HashSet<string>(StringComparer.Ordinal) {
            Path.Combine("Controllers", "MailRelayControllerBase.cs"),
            Path.Combine("Controllers", "AuthorizedMailRelayController.cs"),
        };

        string[] violations = [.. Directory.GetFiles(presentationRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .Where(path => {
                string relative = Path.GetRelativePath(presentationRoot, path);
                return relative.StartsWith($"Features{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false &&
                       allowedControllerFiles.Contains(relative) is false;
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationFeatureFiles_FollowHttpNamingConventions() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");

        var conventions = new[] {
            new { Folder = "Requests", Suffix = "HttpRequest.cs" },
            new { Folder = "Responses", Suffix = "HttpResponse.cs" },
            new { Folder = "Mappings", Suffix = "HttpMappings.cs" },
        };

        string[] violations = [.. conventions
            .SelectMany(convention => Directory.GetFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
                .Where(static path => IsGeneratedPath(path) is false)
                .Where(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Where(path => path.Contains($"{Path.DirectorySeparatorChar}{convention.Folder}{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(path => path.EndsWith(convention.Suffix, StringComparison.Ordinal) is false)
                .Select(path => Path.GetRelativePath(root, path)))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationControllers_UseHttpMappingsInsteadOfConstructingApplicationRequests() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        string[] forbiddenPatterns = [
            "new GetMailRelay",
            "new EnqueueMailRelay",
            "new CreateMailRelay",
            "new RemoveMailRelay",
            "new IngestMailRelay",
            "new CheckMailRelay",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayInfrastructureOptions_AreKeptInInfrastructureOptionsFolderExceptListenOptions() {
        string root = GetRepositoryRoot();
        string[] mailRelayRoots = [
            "FoodDiary.MailRelay.Application",
            "FoodDiary.MailRelay.Client",
            "FoodDiary.MailRelay.Domain",
            "FoodDiary.MailRelay.Infrastructure",
            "FoodDiary.MailRelay.Initializer",
            "FoodDiary.MailRelay.Presentation",
            "FoodDiary.MailRelay.WebApi",
        ];
        var allowedOptionFiles = new HashSet<string>(StringComparer.Ordinal) {
            Path.Combine("FoodDiary.MailRelay.Application", "Options", "MailRelayOptions.cs"),
            Path.Combine("FoodDiary.MailRelay.Client", "Options", "MailRelayClientOptions.cs"),
        };

        string[] violations = [.. mailRelayRoots
            .Select(rootName => Path.Combine(root, rootName))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.GetFiles(directory, "*Options.cs", SearchOption.AllDirectories))
            .Where(path => IsGeneratedPath(path) is false)
            .Where(path => path.StartsWith(Path.Combine(root, "FoodDiary.MailRelay.Infrastructure", "Options"), StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedOptionFiles.Contains(Path.GetRelativePath(root, path)) is false)
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayApplicationInterfaces_AsyncMethodsAcceptCancellationToken() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.MailRelay.Application");

        string[] violations = [.. Directory.GetFiles(applicationRoot, "I*.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayApplication_DoesNotUseFlatServicesFolder() {
        string root = GetRepositoryRoot();
        string servicesRoot = Path.Combine(root, "FoodDiary.MailRelay.Application", "Services");

        Assert.False(
            Directory.Exists(servicesRoot) &&
            Directory.EnumerateFileSystemEntries(servicesRoot).Any(),
            "FoodDiary.MailRelay.Application should stay organized by feature/purpose folders instead of a flat Services folder.");
    }

    [Fact]
    public void PrimaryApiAndInfrastructure_DoNotOwnSmtpDeliveryConfiguration() {
        string root = GetRepositoryRoot();
        string[] sourceRoots = [
            Path.Combine(root, "FoodDiary.Infrastructure"),
            Path.Combine(root, "FoodDiary.Web.Api"),
        ];
        string[] forbiddenPatterns = [
            "EmailDelivery",
            "SmtpClientEmailTransport",
            "ConfigurableEmailTransport",
            "SmtpHealthCheck",
            "SmtpHost",
            "SmtpPort",
            "SmtpUser",
            "SmtpPassword",
            "new SmtpClient",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(sourceRoots, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayRuntimeConfiguration_UsesSeparateDatabase() {
        string root = GetRepositoryRoot();
        string appsettingsPath = Path.Combine(root, "FoodDiary.MailRelay.WebApi", "appsettings.json");
        string composePath = Path.Combine(root, "docker-compose.yml");

        string appsettings = File.ReadAllText(appsettingsPath);
        string compose = File.ReadAllText(composePath);

        Assert.Contains("Database=fooddiary_mailrelay", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("Database=fooddiary;", appsettings, StringComparison.Ordinal);
        Assert.Contains("mailrelay-postgres:", compose, StringComparison.Ordinal);
        Assert.Contains("mailrelay-postgres-data:", compose, StringComparison.Ordinal);
        Assert.Contains("Host=mailrelay-postgres", compose, StringComparison.Ordinal);
        Assert.Contains("MAIL_RELAY_POSTGRES_DB:-fooddiary_mailrelay", compose, StringComparison.Ordinal);
        Assert.Contains("mailrelay-db-init:", compose, StringComparison.Ordinal);
        Assert.Contains("FoodDiary.MailRelay.Initializer/Dockerfile", compose, StringComparison.Ordinal);
        Assert.Contains("service_completed_successfully", compose, StringComparison.Ordinal);
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

    private static IEnumerable<string> GetAsyncMethodSignatures(string path) {
        return CSharpSyntaxReader.ReadMethods(path)
            .Where(static method => method.IsAsyncLike)
            .Select(static method => $"{method.ReturnType} {method.Name}({method.Parameters})");
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

    private static bool IsGeneratedPath(string path) {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }
}
