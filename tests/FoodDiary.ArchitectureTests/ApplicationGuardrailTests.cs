namespace FoodDiary.ArchitectureTests;

public sealed class ApplicationGuardrailTests {
    [Fact]
    public void ApplicationSourceFiles_DoNotUseEnumParseDirectly() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("Enum.Parse(", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationHandlersAndServices_DoNotUseDateTimeUtcNow_Directly() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Path.Combine(applicationRoot, "Common", "Services", "SystemDateTimeProvider.cs"),
        };

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedFiles.Contains(path) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("DateTime.UtcNow", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationServiceInterfaces_AsyncMethodsAcceptCancellationToken() {
        var root = GetRepositoryRoot();
        var servicesRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Services");

        var violations = Directory.GetFiles(servicesRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationPersistenceInterfaces_AsyncMethodsAcceptCancellationToken() {
        var root = GetRepositoryRoot();
        var persistenceRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Persistence");

        var violations = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationCommonServiceInterfaces_StayLimitedToTrueCrossCuttingAbstractions() {
        var root = GetRepositoryRoot();
        var servicesRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Services");
        var allowedFiles = new[] {
            "IDateTimeProvider.cs",
        };

        var actualFiles = Directory.GetFiles(servicesRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationCommonPersistenceInterfaces_DoNotRegrowMovedFeatureSpecificContracts() {
        var root = GetRepositoryRoot();
        var persistenceRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Persistence");
        var forbiddenFiles = new[] {
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

        var violations = forbiddenFiles
            .Where(actualFiles.Contains)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationCommonPersistenceInterfaces_StayLimitedToCurrentCrossFeatureContracts() {
        var root = GetRepositoryRoot();
        var persistenceRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Persistence");
        var allowedFiles = new[] {
            "IProductRepository.cs",
            "IRecipeRepository.cs",
            "IUserRepository.cs",
        };

        var actualFiles = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationSourceFiles_UseFullProductRepositoryOnlyInsideProductsSlice() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");
        var allowedDirectories = new[] {
            Path.Combine(applicationRoot, "Common", "Interfaces", "Persistence"),
            Path.Combine(applicationRoot, "Products"),
        };

        var violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IProductRepository",
            allowedDirectories);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_UseFullRecipeRepositoryOnlyInsideRecipesSlice() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");
        var allowedDirectories = new[] {
            Path.Combine(applicationRoot, "Common", "Interfaces", "Persistence"),
            Path.Combine(applicationRoot, "Recipes"),
        };

        var violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecipeRepository",
            allowedDirectories);

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructurePersistenceRoot_StayLimitedToSharedEfInfrastructureFiles() {
        var root = GetRepositoryRoot();
        var persistenceRoot = Path.Combine(root, "FoodDiary.Infrastructure", "Persistence");
        var allowedFiles = new[] {
            "EfUnitOfWork.cs",
            "FoodDiaryDbContext.cs",
            "FoodDiaryDbContextFactory.cs",
        };

        var actualFiles = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotReferencePresentationOrAspNetTransportTypes() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");
        var forbiddenPatterns = new[] {
            "FoodDiary.Presentation.Api",
            "Microsoft.AspNetCore",
            "IActionResult",
            "ControllerBase",
            "HttpContext",
            "HttpRequest",
            "HttpResponse",
        };

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationProject_DoesNotReferencePresentationProject() {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, "FoodDiary.Application", "FoodDiary.Application.csproj");
        var content = File.ReadAllText(projectPath);

        Assert.DoesNotContain("FoodDiary.Presentation.Api", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseCancellationTokenNone() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("CancellationToken.None", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotDependOnOptionsOrConfiguration() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");
        var forbiddenPatterns = new[] {
            "IOptions<",
            "IOptionsMonitor<",
            "IOptionsSnapshot<",
            "IConfiguration",
            "using Microsoft.Extensions.Options",
            "using Microsoft.Extensions.Configuration",
        };

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
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

    private static string[] FindRepositoryReferenceViolations(
        string repositoryRoot,
        string applicationRoot,
        string typeName,
        IReadOnlyCollection<string> allowedDirectories) {
        return Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedDirectories.Any(directory => path.StartsWith(directory, StringComparison.OrdinalIgnoreCase)) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains(typeName, StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}")
            .ToArray();
    }
}
