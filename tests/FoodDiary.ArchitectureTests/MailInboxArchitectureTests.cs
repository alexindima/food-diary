using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxArchitectureTests {
    [Fact]
    public void MailInboxDomainProject_DoesNotReferenceOtherMailInboxLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.Domain/FoodDiary.MailInbox.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Client", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxApplicationProject_ReferencesDomainOnlyAmongMailInboxLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.Application/FoodDiary.MailInbox.Application.csproj");

        Assert.Contains("FoodDiary.MailInbox.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Client", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxClientProject_DoesNotReferenceMailInboxLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.Client/FoodDiary.MailInbox.Client.csproj");

        Assert.DoesNotContain("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxInfrastructureProject_ReferencesApplicationButNotPresentationOrWebApi() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.Infrastructure/FoodDiary.MailInbox.Infrastructure.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxInitializerProject_ReferencesApplicationAndInfrastructureOnlyAmongMailInboxLayers() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.Contains("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Client", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxPresentationProject_ReferencesApplicationButNotInfrastructureOrWebApi() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.Presentation/FoodDiary.MailInbox.Presentation.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxWebApiProject_IsHostAndReferencesApplicationInfrastructureAndPresentation() {
        HashSet<string> references = GetProjectReferences("FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.Contains("FoodDiary.MailInbox.Infrastructure", references);
        Assert.Contains("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Domain", references);
    }

    [Fact]
    public void MailInboxDomainSource_DoesNotReferenceFrameworkOrInfrastructureTypes() {
        string root = GetRepositoryRoot();
        string domainRoot = Path.Combine(root, "FoodDiary.MailInbox.Domain");
        string[] forbiddenPatterns = [
            "Microsoft.",
            "Npgsql",
            "MailKit",
            "MimeKit",
            "SmtpServer",
            "IOptions",
            "IConfiguration",
            "HttpContext",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(domainRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxApplicationSource_DoesNotReferenceTransportPersistenceOrConfigurationTypes() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.MailInbox.Application");
        string[] forbiddenPatterns = [
            "Microsoft.AspNetCore",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Configuration",
            "IOptions<",
            "IConfiguration",
            "Npgsql",
            "MailKit",
            "MimeKit",
            "SmtpServer",
            "HttpContext",
            "HttpRequest",
            "IEndpointRouteBuilder",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxPresentationSource_DoesNotReferenceInfrastructureLayer() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailInbox.Presentation");
        string[] forbiddenPatterns = [
            "FoodDiary.MailInbox.Infrastructure",
            "Npgsql",
            "MailKit",
            "MimeKit",
            "SmtpServer",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxWebApiProgram_UsesMvcControllersForEndpoints() {
        string root = GetRepositoryRoot();
        string programPath = Path.Combine(root, "FoodDiary.MailInbox.WebApi", "Program.cs");
        string content = File.ReadAllText(programPath);

        Assert.DoesNotContain("MapGet", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", content, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/mail-inbox", content, StringComparison.Ordinal);
        Assert.Contains("MapMailInboxPresentation", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MailInboxPresentationControllers_AreKeptInFeatureFolders() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailInbox.Presentation");
        var allowedControllerFiles = new HashSet<string>(StringComparer.Ordinal) {
            Path.Combine("Controllers", "MailInboxControllerBase.cs"),
        };

        string[] violations = [.. Directory.GetFiles(presentationRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedPath(path))
            .Where(path => {
                string relative = Path.GetRelativePath(presentationRoot, path);
                return !relative.StartsWith($"Features{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                       !allowedControllerFiles.Contains(relative);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxPresentationFeatureFiles_FollowHttpNamingConventions() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.MailInbox.Presentation");

        var conventions = new[] {
            new { Folder = "Requests", Suffix = "HttpRequest.cs" },
            new { Folder = "Responses", Suffix = "HttpResponse.cs" },
            new { Folder = "Mappings", Suffix = "HttpMappings.cs" },
        };

        string[] violations = [.. conventions
            .SelectMany(convention => Directory.GetFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
                .Where(static path => !IsGeneratedPath(path))
                .Where(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Where(path => path.Contains($"{Path.DirectorySeparatorChar}{convention.Folder}{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(path => !path.EndsWith(convention.Suffix, StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(root, path)))
            .OrderBy(static path => path, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxApplicationInterfaces_AsyncMethodsAcceptCancellationToken() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.MailInbox.Application");

        string[] violations = [.. Directory.GetFiles(applicationRoot, "I*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedPath(path))
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => !signature.Contains("CancellationToken", StringComparison.Ordinal))
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxApplication_DoesNotUseFlatServicesFolder() {
        string root = GetRepositoryRoot();
        string servicesRoot = Path.Combine(root, "FoodDiary.MailInbox.Application", "Services");

        Assert.False(
            Directory.Exists(servicesRoot) &&
            Directory.EnumerateFileSystemEntries(servicesRoot).Any(),
            "FoodDiary.MailInbox.Application should stay organized by feature/purpose folders instead of a flat Services folder.");
    }

    [Fact]
    public void MailInboxRuntimeConfiguration_UsesSeparateDatabase() {
        string root = GetRepositoryRoot();
        string appsettingsPath = Path.Combine(root, "FoodDiary.MailInbox.WebApi", "appsettings.json");
        string composePath = Path.Combine(root, "docker-compose.yml");

        string appsettings = File.ReadAllText(appsettingsPath);
        string compose = File.ReadAllText(composePath);

        Assert.Contains("Database=fooddiary_mailinbox", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("Database=fooddiary;", appsettings, StringComparison.Ordinal);
        Assert.Contains("mailinbox-postgres:", compose, StringComparison.Ordinal);
        Assert.Contains("mailinbox-postgres-data:", compose, StringComparison.Ordinal);
        Assert.Contains("Host=mailinbox-postgres", compose, StringComparison.Ordinal);
        Assert.Contains("MAIL_INBOX_POSTGRES_DB:-fooddiary_mailinbox", compose, StringComparison.Ordinal);
        Assert.Contains("mailinbox-db-init:", compose, StringComparison.Ordinal);
        Assert.Contains("FoodDiary.MailInbox.Initializer/Dockerfile", compose, StringComparison.Ordinal);
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
