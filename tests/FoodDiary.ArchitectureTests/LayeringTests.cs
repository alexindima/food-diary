using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

public class LayeringTests {
    [Fact]
    public void DomainProject_DoesNotReference_OtherApplicationLayers() {
        var references = GetProjectReferences("FoodDiary.Domain/FoodDiary.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void ApplicationProject_ReferencesOnly_DomainAndContracts_AmongCoreProjects() {
        var references = GetProjectReferences("FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.Contains("FoodDiary.Domain", references);
        Assert.Contains("FoodDiary.Contracts", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void InfrastructureProject_DoesNotReference_WebApi() {
        var references = GetProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.Contains("FoodDiary.Application", references);
    }

    [Fact]
    public void PresentationApiProject_ReferencesApplicationContractsAndDomain_ButNotInfrastructure() {
        var references = GetProjectReferences("FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj");

        Assert.Contains("FoodDiary.Application", references);
        Assert.Contains("FoodDiary.Contracts", references);
        Assert.Contains("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
    }

    [Fact]
    public void WebApiProject_IsHostAndReferencesPresentationApplicationAndInfrastructure() {
        var references = GetProjectReferences("FoodDiary.Web.Api/FoodDiary.Web.Api.csproj");

        Assert.Contains("FoodDiary.Application", references);
        Assert.Contains("FoodDiary.Infrastructure", references);
        Assert.Contains("FoodDiary.Presentation.Api", references);
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
