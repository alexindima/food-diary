using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

public sealed class MailRelayArchitectureTests {
    [Fact]
    public void MailRelayDomainProject_DoesNotReferenceOtherMailRelayLayers() {
        var references = GetProjectReferences("FoodDiary.MailRelay.Domain/FoodDiary.MailRelay.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.MailRelay.Application", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayApplicationProject_ReferencesDomainOnlyAmongMailRelayLayers() {
        var references = GetProjectReferences("FoodDiary.MailRelay.Application/FoodDiary.MailRelay.Application.csproj");

        Assert.Contains("FoodDiary.MailRelay.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayClientProject_DoesNotReferenceMailRelayLayers() {
        var references = GetProjectReferences("FoodDiary.MailRelay.Client/FoodDiary.MailRelay.Client.csproj");

        Assert.DoesNotContain("FoodDiary.MailRelay.Application", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayInfrastructureProject_ReferencesApplicationButNotPresentationOrWebApi() {
        var references = GetProjectReferences("FoodDiary.MailRelay.Infrastructure/FoodDiary.MailRelay.Infrastructure.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayPresentationProject_ReferencesApplicationAndClientButNotInfrastructureOrWebApi() {
        var references = GetProjectReferences("FoodDiary.MailRelay.Presentation/FoodDiary.MailRelay.Presentation.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.Contains("FoodDiary.MailRelay.Client", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.WebApi", references);
    }

    [Fact]
    public void MailRelayWebApiProject_IsHostAndReferencesApplicationInfrastructureAndPresentation() {
        var references = GetProjectReferences("FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj");

        Assert.Contains("FoodDiary.MailRelay.Application", references);
        Assert.Contains("FoodDiary.MailRelay.Infrastructure", references);
        Assert.Contains("FoodDiary.MailRelay.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailRelay.Domain", references);
    }

    [Fact]
    public void MailRelayDomainSource_DoesNotReferenceFrameworkOrInfrastructureTypes() {
        var root = GetRepositoryRoot();
        var domainRoot = Path.Combine(root, "FoodDiary.MailRelay.Domain");
        var forbiddenPatterns = new[] {
            "Microsoft.",
            "Npgsql",
            "RabbitMQ",
            "MailKit",
            "MimeKit",
            "DnsClient",
            "IOptions",
            "IConfiguration",
            "HttpContext",
        };

        var violations = FindSourcePatternViolations(root, domainRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayApplicationSource_DoesNotReferenceTransportPersistenceOrConfigurationTypes() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.MailRelay.Application");
        var forbiddenPatterns = new[] {
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
        };

        var violations = FindSourcePatternViolations(root, applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationSource_DoesNotReferenceInfrastructureLayer() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        var forbiddenPatterns = new[] {
            "FoodDiary.MailRelay.Infrastructure",
            "MailRelayQueueStore",
            "Npgsql",
            "RabbitMQ",
            "MailKit",
            "DnsClient",
        };

        var violations = FindSourcePatternViolations(root, presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationControllers_UseMediatorInsteadOfApplicationServicesDirectly() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        var forbiddenPatterns = new[] {
            "MailRelayEmailUseCases",
            "MailRelayDeliveryEventIngestionService",
            "IMailRelayQueueStore",
            "IMailRelayDispatchNotifier",
        };

        var violations = FindSourcePatternViolations(root, presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayEmailControllers_RequireRelayApiKeyThroughAuthorizedBaseController() {
        var root = GetRepositoryRoot();
        var emailFeatureRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation", "Features", "Email");

        var violations = Directory.GetFiles(emailFeatureRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .Where(path => File.ReadAllText(path).Contains(": AuthorizedMailRelayController", StringComparison.Ordinal) is false)
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayWebApiProgram_UsesMvcControllersForEmailEndpoints() {
        var root = GetRepositoryRoot();
        var programPath = Path.Combine(root, "FoodDiary.MailRelay.WebApi", "Program.cs");
        var content = File.ReadAllText(programPath);

        Assert.DoesNotContain("MapGet", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", content, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/email", content, StringComparison.Ordinal);
        Assert.Contains("MapMailRelayPresentation", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapMailRelayEndpoints", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MailRelayPresentationControllers_AreKeptInFeatureFolders() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        var allowedControllerFiles = new HashSet<string>(StringComparer.Ordinal) {
            Path.Combine("Controllers", "MailRelayControllerBase.cs"),
            Path.Combine("Controllers", "AuthorizedMailRelayController.cs"),
        };

        var violations = Directory.GetFiles(presentationRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .Where(path => {
                var relative = Path.GetRelativePath(presentationRoot, path);
                return relative.StartsWith($"Features{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false &&
                       allowedControllerFiles.Contains(relative) is false;
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationFeatureFiles_FollowHttpNamingConventions() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");

        var conventions = new[] {
            new { Folder = "Requests", Suffix = "HttpRequest.cs" },
            new { Folder = "Responses", Suffix = "HttpResponse.cs" },
            new { Folder = "Mappings", Suffix = "HttpMappings.cs" },
        };

        var violations = conventions
            .SelectMany(convention => Directory.GetFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
                .Where(static path => IsGeneratedPath(path) is false)
                .Where(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Where(path => path.Contains($"{Path.DirectorySeparatorChar}{convention.Folder}{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(path => path.EndsWith(convention.Suffix, StringComparison.Ordinal) is false)
                .Select(path => Path.GetRelativePath(root, path)))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayPresentationControllers_UseHttpMappingsInsteadOfConstructingApplicationRequests() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailRelay.Presentation");
        var forbiddenPatterns = new[] {
            "new GetMailRelay",
            "new EnqueueMailRelay",
            "new CreateMailRelay",
            "new RemoveMailRelay",
            "new IngestMailRelay",
            "new CheckMailRelay",
        };

        var violations = Directory.GetFiles(presentationRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayInfrastructureOptions_AreKeptInInfrastructureOptionsFolderExceptListenOptions() {
        var root = GetRepositoryRoot();
        var mailRelayRoots = new[] {
            "FoodDiary.MailRelay.Application",
            "FoodDiary.MailRelay.Client",
            "FoodDiary.MailRelay.Domain",
            "FoodDiary.MailRelay.Infrastructure",
            "FoodDiary.MailRelay.Presentation",
            "FoodDiary.MailRelay.WebApi",
        };
        var allowedOptionFiles = new HashSet<string>(StringComparer.Ordinal) {
            Path.Combine("FoodDiary.MailRelay.Application", "Options", "MailRelayOptions.cs"),
            Path.Combine("FoodDiary.MailRelay.Client", "Options", "MailRelayClientOptions.cs"),
        };

        var violations = mailRelayRoots
            .Select(rootName => Path.Combine(root, rootName))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.GetFiles(directory, "*Options.cs", SearchOption.AllDirectories))
            .Where(path => IsGeneratedPath(path) is false)
            .Where(path => path.StartsWith(Path.Combine(root, "FoodDiary.MailRelay.Infrastructure", "Options"), StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedOptionFiles.Contains(Path.GetRelativePath(root, path)) is false)
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayApplicationInterfaces_AsyncMethodsAcceptCancellationToken() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.MailRelay.Application");

        var violations = Directory.GetFiles(applicationRoot, "I*.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayApplication_DoesNotUseFlatServicesFolder() {
        var root = GetRepositoryRoot();
        var servicesRoot = Path.Combine(root, "FoodDiary.MailRelay.Application", "Services");

        Assert.False(
            Directory.Exists(servicesRoot) &&
            Directory.EnumerateFileSystemEntries(servicesRoot).Any(),
            "FoodDiary.MailRelay.Application should stay organized by feature/purpose folders instead of a flat Services folder.");
    }

    [Fact]
    public void PrimaryApiAndInfrastructure_DoNotOwnSmtpDeliveryConfiguration() {
        var root = GetRepositoryRoot();
        var sourceRoots = new[] {
            Path.Combine(root, "FoodDiary.Infrastructure"),
            Path.Combine(root, "FoodDiary.Web.Api"),
        };
        var forbiddenPatterns = new[] {
            "EmailDelivery",
            "SmtpClientEmailTransport",
            "ConfigurableEmailTransport",
            "SmtpHealthCheck",
            "SmtpHost",
            "SmtpPort",
            "SmtpUser",
            "SmtpPassword",
            "new SmtpClient",
        };

        var violations = sourceRoots
            .SelectMany(sourceRoot => FindSourcePatternViolations(root, sourceRoot, forbiddenPatterns))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailRelayRuntimeConfiguration_UsesSeparateDatabase() {
        var root = GetRepositoryRoot();
        var appsettingsPath = Path.Combine(root, "FoodDiary.MailRelay.WebApi", "appsettings.json");
        var composePath = Path.Combine(root, "docker-compose.yml");

        var appsettings = File.ReadAllText(appsettingsPath);
        var compose = File.ReadAllText(composePath);

        Assert.Contains("Database=fooddiary_mailrelay", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("Database=fooddiary;", appsettings, StringComparison.Ordinal);
        Assert.Contains("mailrelay-postgres:", compose, StringComparison.Ordinal);
        Assert.Contains("mailrelay-postgres-data:", compose, StringComparison.Ordinal);
        Assert.Contains("Host=mailrelay-postgres", compose, StringComparison.Ordinal);
        Assert.Contains("MAIL_RELAY_POSTGRES_DB:-fooddiary_mailrelay", compose, StringComparison.Ordinal);
    }

    private static HashSet<string> GetProjectReferences(string relativeProjectPath) {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => GetProjectNameFromReference(value!))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string[] FindSourcePatternViolations(
        string repositoryRoot,
        string sourceRoot,
        IReadOnlyCollection<string> forbiddenPatterns) {
        if (!Directory.Exists(sourceRoot)) {
            return [];
        }

        return Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}")
            .ToArray();
    }

    private static IEnumerable<string> GetAsyncMethodSignatures(string path) {
        var content = File.ReadAllText(path);
        var normalized = content.ReplaceLineEndings("\n");
        var matches = System.Text.RegularExpressions.Regex.Matches(
            normalized,
            @"Task(?:<[^;]+?>)?\s+\w+Async\s*\((.*?)\)\s*;",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        foreach (System.Text.RegularExpressions.Match match in matches) {
            yield return match.Value.ReplaceLineEndings(" ").Replace('\n', ' ').Trim();
        }
    }

    private static string GetRepositoryRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            var solutionPath = Path.Combine(current.FullName, "FoodDiary.slnx");
            if (File.Exists(solutionPath)) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private static string GetProjectNameFromReference(string includeValue) {
        var normalized = includeValue.Replace('\\', '/');
        var fileName = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static bool IsGeneratedPath(string path) {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }
}
