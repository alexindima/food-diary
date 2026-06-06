namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationGuardrailTests {
    [Fact]
    public void ApplicationSourceFiles_DoNotUseEnumParseDirectly() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, ["Enum.Parse("]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationHandlersAndServices_DoNotUseDateTimeUtcNow_Directly() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, ["DateTime.UtcNow"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationServiceInterfaces_AsyncMethodsAcceptCancellationToken() {
        string root = GetRepositoryRoot();
        string servicesRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Services");

        string[] violations = Directory.GetFiles(servicesRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationPersistenceInterfaces_AsyncMethodsAcceptCancellationToken() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Persistence");

        string[] violations = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationCommonServiceInterfaces_StayLimitedToTrueCrossCuttingAbstractions() {
        string root = GetRepositoryRoot();
        string servicesRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Services");
        string[] allowedFiles = new[] {
            "IDateTimeProvider.cs",
        };

        string?[] actualFiles = Directory.GetFiles(servicesRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationAbstractionsCommonPersistenceInterfaces_DoNotRegrowMovedFeatureSpecificContracts() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Persistence");
        string[] forbiddenFiles = new[] {
            "IAiUsageRepository.cs",
            "ICycleRepository.cs",
            "IDailyAdviceRepository.cs",
            "IEmailTemplateRepository.cs",
            "IHydrationEntryRepository.cs",
            "IImageAssetRepository.cs",
            "IMealRepository.cs",
            "IRecentItemRepository.cs",
            "IShoppingListRepository.cs",
            "IWaistEntryRepository.cs",
            "IWeightEntryRepository.cs",
        };

        var actualFiles = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        string[] violations = forbiddenFiles
            .Where(actualFiles.Contains)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractionsCommonPersistenceInterfaces_StayLimitedToCurrentCrossFeatureContracts() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Persistence");
        string[] allowedFiles = new[] {
            "IProductRepository.cs",
            "IRecipeRepository.cs",
            "IUserRepository.cs",
        };

        string?[] actualFiles = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationSourceFiles_UseFullProductRepositoryOnlyInsideProductsSlice() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] allowedDirectories = new[] {
            Path.Combine(applicationRoot, "Products"),
            Path.Combine(applicationRoot, "FavoriteProducts"),
            Path.Combine(applicationRoot, "Usda"),
        };

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IProductRepository",
            allowedDirectories);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_UseFullRecipeRepositoryOnlyInsideRecipesSlice() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] allowedDirectories = new[] {
            Path.Combine(applicationRoot, "Recipes"),
            Path.Combine(applicationRoot, "FavoriteRecipes"),
            Path.Combine(applicationRoot, "RecipeComments"),
            Path.Combine(applicationRoot, "RecipeLikes"),
        };

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecipeRepository",
            allowedDirectories);

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructurePersistenceRoot_StayLimitedToSharedEfInfrastructureFiles() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Infrastructure", "Persistence");
        string[] allowedFiles = new[] {
            "EfUnitOfWork.cs",
            "FoodDiaryDbContext.cs",
            "FoodDiaryDbContextFactory.cs",
        };

        string?[] actualFiles = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotReferencePresentationOrAspNetTransportTypes() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] forbiddenPatterns = new[] {
            "FoodDiary.Presentation.Api",
            "Microsoft.AspNetCore",
            "IActionResult",
            "ControllerBase",
            "HttpContext",
            "HttpRequest",
            "HttpResponse",
        };

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationProject_DoesNotReferencePresentationProject() {
        string root = GetRepositoryRoot();
        string projectPath = Path.Combine(root, "FoodDiary.Application", "FoodDiary.Application.csproj");
        string content = File.ReadAllText(projectPath);

        Assert.DoesNotContain("FoodDiary.Presentation.Api", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseCancellationTokenNone() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, ["CancellationToken.None"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotDependOnOptionsOrConfiguration() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] forbiddenPatterns = new[] {
            "IOptions<",
            "IOptionsMonitor<",
            "IOptionsSnapshot<",
            "IConfiguration",
            "using Microsoft.Extensions.Options",
            "using Microsoft.Extensions.Configuration",
        };

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
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

    private static IEnumerable<string> GetAsyncMethodSignatures(string path) {
        return CSharpSyntaxReader.ReadMethods(path)
            .Where(static method => method.IsAsyncLike)
            .Select(static method => $"{method.ReturnType} {method.Name}({method.Parameters})");
    }

    private static string[] FindRepositoryReferenceViolations(
        string repositoryRoot,
        string applicationRoot,
        string typeName,
        IReadOnlyCollection<string> allowedDirectories) {
        return SourceScanner.SourceFiles(applicationRoot)
            .Where(path => allowedDirectories.Any(directory => path.StartsWith(directory, StringComparison.OrdinalIgnoreCase)) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains(typeName, StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}")
            .ToArray();
    }
}
