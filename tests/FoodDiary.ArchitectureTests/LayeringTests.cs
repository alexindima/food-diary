using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class LayeringTests {
    [Fact]
    public void SourceScanner_IgnoresNamespaceTextInsideStringLiterals() {
        string path = Path.GetTempFileName();
        try {
            File.WriteAllText(path, "var route = \"FoodDiary.Web.Api/\";\nusingType = FoodDiary.Web.Api.Endpoint;");

            string[] lines = SourceScanner.ReadCodeLines(path);

            Assert.DoesNotContain("FoodDiary.Web.Api", lines[0], StringComparison.Ordinal);
            Assert.Contains("FoodDiary.Web.Api", lines[1], StringComparison.Ordinal);
        } finally {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Products")]
    [InlineData("Users")]
    [InlineData("Usda")]
    [InlineData("Cycles")]
    public void DomainProject_DoesNotReference_OtherApplicationLayers(string module) {
        HashSet<string> references = GetProjectReferences($"Modules/{module}/Domain/FoodDiary.Modules.{module}.Domain.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Contracts", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void ApplicationAbstractionsProject_ReferencesOnly_DomainAmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("Shared/FoodDiary.Application.Contracts/FoodDiary.Application.Contracts.csproj");

        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
    }

    [Fact]
    public void ApplicationRuntimeProject_ReferencesOnly_ApplicationContractsAmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.Contains("FoodDiary.Application.Contracts", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Fact]
    public void InfrastructureProject_ReferencesOnly_DomainAndApplicationAbstractions_AmongCoreProjects() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.Contains("FoodDiary.Application.Contracts", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Application", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
    }

    [Fact]
    public void MailRelayAdapter_ReferencesOnlyItsContractAndClientBoundaries() {
        HashSet<string> references = GetProjectReferences("Shared/FoodDiary.Email.MailRelay/FoodDiary.Email.MailRelay.csproj");

        Assert.Equal(["FoodDiary.Email.Contracts", "FoodDiary.MailRelay.Client"], references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain("FoodDiary.Presentation.Api", references);
    }

    [Theory]
    [InlineData("Modules/Billing/Infrastructure/Providers")]
    [InlineData("Modules/Ai/Infrastructure/Providers")]
    [InlineData("Modules/Wearables/Infrastructure/Providers")]
    [InlineData("Modules/Usda/Infrastructure/Providers")]
    [InlineData("Modules/OpenFoodFacts/Infrastructure/Providers")]
    [InlineData("Modules/Identity/Infrastructure/Providers")]
    [InlineData("Modules/Images/Infrastructure/Providers")]
    public void IntegrationsProject_UsesTimeProviderInsteadOfDirectUtcNow(string providerPath) {
        string integrationsRoot = ArchitectureTestPaths.FromRoot(providerPath);

        string[] violations = SourceScanner.FindLinePatternViolations(integrationsRoot, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Modules/Billing/Infrastructure/Providers")]
    [InlineData("Modules/Ai/Infrastructure/Providers")]
    [InlineData("Modules/Wearables/Infrastructure/Providers")]
    [InlineData("Modules/Usda/Infrastructure/Providers")]
    [InlineData("Modules/OpenFoodFacts/Infrastructure/Providers")]
    [InlineData("Modules/Identity/Infrastructure/Providers")]
    [InlineData("Modules/Images/Infrastructure/Providers")]
    public void IntegrationsSource_DoesNotReferenceConcreteApplicationHostPresentationOrServerMailLayers(string providerPath) {
        string integrationsRoot = ArchitectureTestPaths.FromRoot(providerPath);
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

    [Theory]
    [InlineData("Modules/Billing/Infrastructure/Providers")]
    [InlineData("Modules/Ai/Infrastructure/Providers")]
    [InlineData("Modules/Wearables/Infrastructure/Providers")]
    [InlineData("Modules/Usda/Infrastructure/Providers")]
    [InlineData("Modules/OpenFoodFacts/Infrastructure/Providers")]
    [InlineData("Modules/Identity/Infrastructure/Providers")]
    [InlineData("Modules/Images/Infrastructure/Providers")]
    public void IntegrationsOptions_AreKeptInOptionsFolder(string providerPath) {
        string integrationsRoot = ArchitectureTestPaths.FromRoot(providerPath);
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
    public void InfrastructureProject_PackageReferencesStayLimitedToPersistenceAndTechnicalImplementations() {
        string[] allowedPackages = [
            "Microsoft.AspNetCore.DataProtection",
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
            "Npgsql.EntityFrameworkCore.PostgreSQL",
        ];

        string[] packages = ProjectReferenceReader.ReadPackageReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");

        Assert.Equal(allowedPackages, packages);
    }

    [Fact]
    public void SharedBuildSettings_PruneNonTargetSkiaSharpNativeAssets() {
        string propsPath = ArchitectureTestPaths.FromRoot("Directory.Build.props");
        var document = XDocument.Load(propsPath);
        XElement target = Assert.Single(document.Descendants("Target"), element =>
            string.Equals(
                element.Attribute("Name")?.Value,
                "PruneNonTargetSkiaSharpNativeAssets",
                StringComparison.Ordinal));

        Assert.Equal("ResolveLockFileCopyLocalFiles", target.Attribute("AfterTargets")?.Value);
        string[] conditions = [.. target.Descendants()
            .Select(element => element.Attribute("Condition")?.Value)
            .Where(static condition => !string.IsNullOrWhiteSpace(condition))
            .Select(static condition => condition!)];

        Assert.Multiple(
            () => Assert.Contains(conditions, condition => condition.Contains("SkiaSharp.NativeAssets.Linux.NoDependencies", StringComparison.Ordinal)),
            () => Assert.Contains(conditions, condition => condition.Contains("SkiaSharp.NativeAssets.macOS", StringComparison.Ordinal)),
            () => Assert.Contains(conditions, condition => condition.Contains("SkiaSharp.NativeAssets.Win32", StringComparison.Ordinal)),
            () => Assert.Contains(conditions, condition => condition.Contains(".pdb", StringComparison.Ordinal)));
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
            "services.AddAuditPersistence();",
            "services.AddEmailPersistence();",
            "services.AddAuthenticationInfrastructure();",
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
    public void InfrastructureLegacyFeatureCompositionFiles_DoNotReturn() {
        string[] obsoletePaths = [
            "DependencyInjection.Repositories.cs",
            "DependencyInjection.Food.cs",
            "DependencyInjection.Moderation.cs",
        ];

        Assert.All(obsoletePaths, path => Assert.False(File.Exists(
            ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", path))));
    }

    [Fact]
    public void PresentationKernel_ReferencesOnlySharedTransportDependencies() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Runtime", references);
        Assert.DoesNotContain("FoodDiary.Modules.Users.Application", references);
        Assert.DoesNotContain("FoodDiary.Domain", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
        Assert.DoesNotContain("FoodDiary.Web.Api", references);
        Assert.DoesNotContain("FoodDiary.Infrastructure", references);
        Assert.DoesNotContain(references, reference => reference.EndsWith(".Presentation", StringComparison.Ordinal));
    }

    [Fact]
    public void ModulePresentationProjects_ReferenceTheKernelButNotInfrastructureOrHosts() {
        string root = GetRepositoryRoot();
        string[] projectFiles = Directory.GetFiles(Path.Combine(root, "Modules"), "*.Presentation.csproj", SearchOption.AllDirectories);
        var allowedCrossModuleApplicationReferences = new HashSet<string>(StringComparer.Ordinal) {
            "Users -> FoodDiary.Modules.Dietologist.Presentation.Contracts",
        };

        Assert.Equal(32, projectFiles.Length);

        string[] violations = [.. projectFiles.SelectMany(projectFile => {
            string relativePath = Path.GetRelativePath(root, projectFile).Replace('\\', '/');
            string module = new DirectoryInfo(Path.GetDirectoryName(projectFile)!).Parent!.Name;
            HashSet<string> references = GetProjectReferences(relativePath);
            var errors = new List<string>();
            if (!references.Contains("FoodDiary.Presentation.Api")) {
                errors.Add($"{relativePath} does not reference FoodDiary.Presentation.Api");
            }

            errors.AddRange(references
                .Where(reference => reference.Contains(".Infrastructure", StringComparison.Ordinal) ||
                                    reference.EndsWith(".Web.Api", StringComparison.Ordinal) ||
                                    reference.EndsWith(".WebApi", StringComparison.Ordinal) ||
                                    reference.EndsWith(".JobManager", StringComparison.Ordinal) ||
                                    reference.EndsWith(".Initializer", StringComparison.Ordinal))
                .Select(reference => $"{relativePath} references forbidden {reference}"));
            errors.AddRange(references
                .Where(reference => reference.StartsWith("FoodDiary.Application.", StringComparison.Ordinal) ||
                                    reference.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal))
                .Where(reference => !reference.StartsWith($"FoodDiary.Application.{module}", StringComparison.Ordinal))
                .Where(reference => !reference.StartsWith($"FoodDiary.Modules.{module}.", StringComparison.Ordinal))
                .Where(reference => !reference.EndsWith(".Presentation", StringComparison.Ordinal))
                .Where(reference => !reference.EndsWith(".Contracts", StringComparison.Ordinal))
                .Where(reference => !allowedCrossModuleApplicationReferences.Contains($"{module} -> {reference}"))
                .Select(reference => $"{relativePath} crosses ownership through {reference}"));
            return errors;
        }).Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void WebApiHost_ReferencesEveryModulePresentationProject() {
        string root = GetRepositoryRoot();
        string[] expected = [.. Directory.GetFiles(Path.Combine(root, "Modules"), "*.Presentation.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Order(StringComparer.Ordinal)];
        string[] actual = [.. GetProjectReferences("FoodDiary.Web.Api/FoodDiary.Web.Api.csproj")
            .Where(reference => reference.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal) &&
                                reference.EndsWith(".Presentation", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        string compositionSource = File.ReadAllText(Path.Combine(root, "FoodDiary.Web.Api", "Extensions", "ApiServiceCollectionExtensions.cs"));

        Assert.Multiple(
            () => Assert.Equal(expected, actual),
            () => Assert.All(expected, projectName => {
                string module = projectName["FoodDiary.Modules.".Length..^".Presentation".Length];
                Assert.Contains($".Add{module}Presentation()", compositionSource, StringComparison.Ordinal);
            }));
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseDomainNamespaces() {
        string root = GetRepositoryRoot();
        string[] presentationRoots = [
            Path.Combine(root, "FoodDiary.Presentation.Api"),
            .. Directory.GetDirectories(Path.Combine(root, "Modules"), "Presentation", SearchOption.AllDirectories)
                .Where(path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoots, [
            "using FoodDiary.Domain",
            "FoodDiary.Domain.",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void WebApiProject_IsHostAndReferencesPresentationApplicationAndInfrastructure() {
        HashSet<string> references = GetProjectReferences("FoodDiary.Web.Api/FoodDiary.Web.Api.csproj");

        Assert.Contains("FoodDiary.Application.Runtime", references);
        Assert.Contains("FoodDiary.Infrastructure", references);
        Assert.Contains("FoodDiary.Email.MailRelay", references);
        Assert.DoesNotContain("FoodDiary.Integrations", references);
        Assert.Contains("FoodDiary.Presentation.Api", references);
        Assert.DoesNotContain("FoodDiary.Resources", references);
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

        Assert.Contains("BaseApiController", names, StringComparer.Ordinal);
        Assert.Contains("AuthorizedController", names, StringComparer.Ordinal);
        Assert.Equal(2, names.Length);
    }

    [Fact]
    public void PresentationApi_EndpointControllersLiveUnderFeatures() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");
        string controllersRoot = Path.Combine(presentationRoot, "Controllers");
        string[] presentationRoots = [
            presentationRoot,
            .. Directory.GetDirectories(Path.Combine(root, "Modules"), "Presentation", SearchOption.AllDirectories)
                .Where(path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
        ];

        string[] violations = [.. presentationRoots.SelectMany(path => Directory.GetFiles(path, "*Controller.cs", SearchOption.AllDirectories))
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
