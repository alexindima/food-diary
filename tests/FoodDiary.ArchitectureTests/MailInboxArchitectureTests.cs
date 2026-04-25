using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

public sealed class MailInboxArchitectureTests {
    [Fact]
    public void MailInboxDomainProject_DoesNotReferenceOtherMailInboxLayers() {
        var references = GetProjectReferences("FoodDiary.MailInbox.Domain/FoodDiary.MailInbox.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxApplicationProject_ReferencesDomainOnlyAmongMailInboxLayers() {
        var references = GetProjectReferences("FoodDiary.MailInbox.Application/FoodDiary.MailInbox.Application.csproj");

        Assert.Contains("FoodDiary.MailInbox.Domain", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxInfrastructureProject_ReferencesApplicationButNotPresentationOrWebApi() {
        var references = GetProjectReferences("FoodDiary.MailInbox.Infrastructure/FoodDiary.MailInbox.Infrastructure.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxInitializerProject_ReferencesApplicationAndInfrastructureOnlyAmongMailInboxLayers() {
        var references = GetProjectReferences("FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.Contains("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxPresentationProject_ReferencesApplicationButNotInfrastructureOrWebApi() {
        var references = GetProjectReferences("FoodDiary.MailInbox.Presentation/FoodDiary.MailInbox.Presentation.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.WebApi", references);
    }

    [Fact]
    public void MailInboxWebApiProject_IsHostAndReferencesApplicationInfrastructureAndPresentation() {
        var references = GetProjectReferences("FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj");

        Assert.Contains("FoodDiary.MailInbox.Application", references);
        Assert.Contains("FoodDiary.MailInbox.Infrastructure", references);
        Assert.Contains("FoodDiary.MailInbox.Presentation", references);
        Assert.DoesNotContain("FoodDiary.MailInbox.Domain", references);
    }

    [Fact]
    public void MailInboxDomainSource_DoesNotReferenceFrameworkOrInfrastructureTypes() {
        var root = GetRepositoryRoot();
        var domainRoot = Path.Combine(root, "FoodDiary.MailInbox.Domain");
        var forbiddenPatterns = new[] {
            "Microsoft.",
            "Npgsql",
            "MailKit",
            "MimeKit",
            "SmtpServer",
            "IOptions",
            "IConfiguration",
            "HttpContext",
        };

        var violations = FindSourcePatternViolations(root, domainRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxApplicationSource_DoesNotReferenceTransportPersistenceOrConfigurationTypes() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.MailInbox.Application");
        var forbiddenPatterns = new[] {
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
        };

        var violations = FindSourcePatternViolations(root, applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxPresentationSource_DoesNotReferenceInfrastructureLayer() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailInbox.Presentation");
        var forbiddenPatterns = new[] {
            "FoodDiary.MailInbox.Infrastructure",
            "Npgsql",
            "MailKit",
            "MimeKit",
            "SmtpServer",
        };

        var violations = FindSourcePatternViolations(root, presentationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxWebApiProgram_UsesMvcControllersForEndpoints() {
        var root = GetRepositoryRoot();
        var programPath = Path.Combine(root, "FoodDiary.MailInbox.WebApi", "Program.cs");
        var content = File.ReadAllText(programPath);

        Assert.DoesNotContain("MapGet", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", content, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/mail-inbox", content, StringComparison.Ordinal);
        Assert.Contains("MapMailInboxPresentation", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MailInboxPresentationControllers_AreKeptInFeatureFolders() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailInbox.Presentation");
        var allowedControllerFiles = new HashSet<string>(StringComparer.Ordinal) {
            Path.Combine("Controllers", "MailInboxControllerBase.cs"),
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
    public void MailInboxPresentationFeatureFiles_FollowHttpNamingConventions() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.MailInbox.Presentation");

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
    public void MailInboxApplicationInterfaces_AsyncMethodsAcceptCancellationToken() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.MailInbox.Application");

        var violations = Directory.GetFiles(applicationRoot, "I*.cs", SearchOption.AllDirectories)
            .Where(static path => IsGeneratedPath(path) is false)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MailInboxApplication_DoesNotUseFlatServicesFolder() {
        var root = GetRepositoryRoot();
        var servicesRoot = Path.Combine(root, "FoodDiary.MailInbox.Application", "Services");

        Assert.False(
            Directory.Exists(servicesRoot) &&
            Directory.EnumerateFileSystemEntries(servicesRoot).Any(),
            "FoodDiary.MailInbox.Application should stay organized by feature/purpose folders instead of a flat Services folder.");
    }

    [Fact]
    public void MailInboxRuntimeConfiguration_UsesSeparateDatabase() {
        var root = GetRepositoryRoot();
        var appsettingsPath = Path.Combine(root, "FoodDiary.MailInbox.WebApi", "appsettings.json");
        var composePath = Path.Combine(root, "docker-compose.yml");

        var appsettings = File.ReadAllText(appsettingsPath);
        var compose = File.ReadAllText(composePath);

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
