using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

public class LayeringTests {
    [Fact]
    public void DomainProject_DoesNotReference_OtherApplicationLayers() {
        var references = GetProjectReferences("FoodDiary.Domain/FoodDiary.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Abstractions", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void ApplicationAbstractionsProject_ReferencesOnly_DomainAmongCoreProjects() {
        var references = GetProjectReferences("FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj");

        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
    }

    [Fact]
    public void ApplicationProject_ReferencesOnly_DomainAndContracts_AmongCoreProjects() {
        var references = GetProjectReferences("FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void InfrastructureProject_ReferencesOnly_DomainAndApplicationAbstractions_AmongCoreProjects() {
        var references = GetProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
    }

    [Fact]
    public void IntegrationsProject_ReferencesApplicationAbstractionsAndExternalClients_ButNotInfrastructure() {
        var references = GetProjectReferences("FoodDiary.Integrations/FoodDiary.Integrations.csproj");

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
    public void InfrastructureProject_DoesNotReferenceExternalProviderPackages() {
        var packages = GetPackageReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.DoesNotContain("AWSSDK.S3", packages);
        Assert.DoesNotContain("Stripe.net", packages);
        Assert.DoesNotContain("WebPush", packages);
        Assert.DoesNotContain("Microsoft.AspNetCore.WebUtilities", packages);
        Assert.DoesNotContain("Microsoft.IdentityModel.Protocols.OpenIdConnect", packages);
    }

    [Fact]
    public void PresentationApiProject_ReferencesApplication_ButNotInfrastructure() {
        var references = GetProjectReferences("FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj");

        Assert.Contains("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
    }

    [Fact]
    public void ResourcesProject_ReferencesOnly_ApplicationAmongCoreProjects() {
        var references = GetProjectReferences("FoodDiary.Resources/FoodDiary.Resources.csproj");

        Assert.Contains("FoodDiary.Application.Abstractions", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseDomainNamespaces() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");

        var violations = Directory.GetFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.TrimStart().StartsWith("using FoodDiary.Domain", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void WebApiProject_IsHostAndReferencesPresentationApplicationAndInfrastructure() {
        var references = GetProjectReferences("FoodDiary.Web.Api/FoodDiary.Web.Api.csproj");

        Assert.Contains("FoodDiary.Application", references);
        Assert.Contains("FoodDiary.Infrastructure", references);
        Assert.Contains("FoodDiary.Integrations", references);
        Assert.Contains("FoodDiary.Presentation.Api", references);
        Assert.Contains("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
    }

    [Fact]
    public void JobManagerProject_DoesNotReference_WebApi() {
        var references = GetProjectReferences("FoodDiary.JobManager/FoodDiary.JobManager.csproj");

        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void TelegramBotProject_DoesNotReference_CoreProjects() {
        var references = GetProjectReferences("FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj");

        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void PresentationApi_OnlyBaseControllersRemainInControllersFolder() {
        var root = GetRepositoryRoot();
        var controllerFiles = Directory.GetFiles(Path.Combine(root, "FoodDiary.Presentation.Api", "Controllers"), "*Controller.cs");
        var names = controllerFiles.Select(Path.GetFileNameWithoutExtension).ToArray();

        Assert.Contains("BaseApiController", names);
        Assert.Contains("AuthorizedController", names);
        Assert.Equal(2, names.Length);
    }

    private static HashSet<string> GetProjectReferences(string relativeProjectPath) {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        var references = document.Descendants("ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => GetProjectNameFromReference(value!))
            .ToHashSet(StringComparer.Ordinal);

        return references;
    }

    private static HashSet<string> GetPackageReferences(string relativeProjectPath) {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return document.Descendants("PackageReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToHashSet(StringComparer.Ordinal);
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
}
