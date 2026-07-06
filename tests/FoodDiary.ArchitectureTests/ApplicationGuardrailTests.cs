using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationGuardrailTests {
    [Fact]
    public void ApplicationProject_StaysDependencyLightweight() {
        const string relativeProjectPath = "FoodDiary.Application/FoodDiary.Application.csproj";
        string[] allowedProjectReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ];
        string[] allowedPackageReferences = [
            "FluentValidation",
            "FluentValidation.DependencyInjectionExtensions",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Logging.Abstractions",
        ];

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.Equal(allowedProjectReferences, projectReferences);
        Assert.Equal(allowedPackageReferences, packageReferences);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseEnumParseDirectly() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, [
            "Enum.Parse(",
            "Enum.Parse<",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationQueryHandlers_DoNotUseReadRepositoriesOrDomainEntities() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] queryHandlers = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => path.EndsWith("QueryHandler.cs", StringComparison.Ordinal))];

        string[] violations = [
            .. FindReferencesInFiles(root, queryHandlers, "ReadRepository"),
            .. FindReferencesInFiles(root, queryHandlers, "FoodDiary.Domain.Entities"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationEngagementCalculators_UseReadModelsInsteadOfDomainAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] calculatorFiles = [
            Path.Combine(applicationRoot, "Tdee", "Services", "TdeeCalculator.cs"),
            Path.Combine(applicationRoot, "WeeklyCheckIn", "Services", "WeeklyCheckInCalculator.cs"),
            Path.Combine(applicationRoot, "Gamification", "Services", "GamificationCalculator.cs"),
        ];
        string[] forbiddenPatterns = [
            "FoodDiary.Domain.Entities.Meals",
            "FoodDiary.Domain.Entities.Tracking",
            "IReadOnlyList<Meal>",
            "IReadOnlyList<WeightEntry>",
            "IReadOnlyList<WaistEntry>",
            "IReadOnlyList<ExerciseEntry>",
        ];

        string[] violations = [.. forbiddenPatterns
            .SelectMany(pattern => FindReferencesInFiles(root, calculatorFiles, pattern))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationFeatures_DoNotUseLegacyCommandsCommonFolders() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [.. Directory.GetDirectories(applicationRoot, "Common", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(
                $"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}Common",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedSmallApplicationFeatureHandlersAndValidators_AreSealed() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedSlices = [
            "Ai",
            "Admin",
            "Authentication",
            "ContentReports",
            "Consumptions",
            "Cycles",
            "DailyAdvices",
            "Dashboard",
            "Dietologist",
            "Exercises",
            "Export",
            "FavoriteMeals",
            "FavoriteProducts",
            "FavoriteRecipes",
            "Fasting",
            "Gamification",
            "Hydration",
            "Lessons",
            "MealPlans",
            "Notifications",
            "OpenFoodFacts",
            "Products",
            "RecipeComments",
            "RecipeLikes",
            "Recipes",
            "ShoppingLists",
            "Statistics",
            "Tdee",
            "Usda",
            "Users",
            "WaistEntries",
            "Wearables",
            "WeightEntries",
            "WeeklyCheckIn",
        ];

        string[] violations = [.. migratedSlices
            .Select(slice => Path.Combine(applicationRoot, slice))
            .SelectMany(SourceScanner.SourceFiles)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}EventHandlers{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
                path.EndsWith("Handler.cs", StringComparison.Ordinal) ||
                path.EndsWith("Validator.cs", StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry =>
                entry.line.Contains("public class ", StringComparison.Ordinal) ||
                entry.line.Contains("public abstract class ", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationDomainEventHandlers_AreSealed() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}EventHandlers{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => path.EndsWith("Handler.cs", StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains("public class ", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationServicesBuildersAndFactories_AreSealedOrStatic() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path =>
                path.EndsWith("Service.cs", StringComparison.Ordinal) ||
                path.EndsWith("Builder.cs", StringComparison.Ordinal) ||
                path.EndsWith("Factory.cs", StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry =>
                entry.line.Contains("public class ", StringComparison.Ordinal) ||
                entry.line.Contains("internal class ", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationConcreteClasses_AreSealedOrStatic() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = SourceScanner.FindUnsealedConcreteClassDeclarations([applicationRoot]);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_UseSharedEnumParsersForTryParse() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string allowedRoot = Path.Combine(applicationRoot, "Common", "Validation");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => entry.line.Contains("Enum.TryParse", StringComparison.Ordinal))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

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
    public void ApplicationAsyncMethods_UseAsyncSuffixExceptFrameworkEntrypoints() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .SelectMany(path => CSharpSyntaxReader.ReadMethods(path)
                .Where(static method => method.IsAsyncLike)
                .Where(static method => !method.Name.EndsWith("Async", StringComparison.Ordinal))
                .Where(static method => !string.Equals(method.Name, "Handle", StringComparison.Ordinal))
                .Select(method => method.Format(root)))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotSuppressCancellationTokens() {
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");
        string[] allowedFiles = [
            Path.Combine(applicationRoot, "Common", "Behaviors", "CommandTransactionBehavior.cs"),
            Path.Combine(applicationRoot, "Fasting", "Services", "FastingNotificationScheduler.cs"),
        ];
        string[] forbiddenPatterns = [
            "CancellationToken.None",
            "default(CancellationToken)",
            "new CancellationToken(",
        ];

        var allowed = allowedFiles
            .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] violations = [.. SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns)
            .Where(violation => !allowed.Contains(violation.Split(':')[0]))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationCommonServiceInterfaces_StayLimitedToTrueCrossCuttingAbstractions() {
        string root = GetRepositoryRoot();
        string servicesRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Services");
        string[] allowedFiles = [];

        string?[] actualFiles = [.. GetFilesIfDirectoryExists(servicesRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void ApplicationAbstractionsCommonPersistenceInterfaces_DoNotRegrowMovedFeatureSpecificContracts() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Persistence");
        string[] forbiddenFiles = [
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
        ];

        var actualFiles = GetFilesIfDirectoryExists(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        string[] violations = [.. forbiddenFiles
            .Where(actualFiles.Contains)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractionsCommonPersistenceInterfaces_StayLimitedToCurrentCrossFeatureContracts() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Persistence");
        string[] allowedFiles = [];

        string?[] actualFiles = [.. GetFilesIfDirectoryExists(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(allowedFiles, actualFiles);
    }

    [Fact]
    public void UserRepositoryContract_DoesNotRegrowRoleMembershipWrites() {
        string root = GetRepositoryRoot();
        string userRepositoryPath = Path.Combine(
            root,
            "FoodDiary.Application.Abstractions",
            "Users",
            "Common",
            "IUserRepository.cs");
        string source = File.ReadAllText(userRepositoryPath);
        string[] forbiddenPatterns = [
            "EnsureRoleAsync",
            "RemoveRoleAsync",
            "IUserRoleMembershipService",
            "GetRolesByNamesAsync",
            "EnsureRolesByNamesAsync",
            "IUserRoleCatalogService",
        ];

        string[] violations = [.. forbiddenPatterns
            .Where(pattern => source.Contains(pattern, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IUserRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseBroadUserReadRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IUserReadRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationAbstractionsErrorsRoot_StaysAsThinPartialFacade() {
        string root = GetRepositoryRoot();
        string resultsRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Abstractions", "Results");
        string errorsRootPath = Path.Combine(resultsRoot, "Errors.cs");

        string source = File.ReadAllText(errorsRootPath);
        Assert.DoesNotContain("public static class ", source, StringComparison.Ordinal);

        string[] featureErrorFiles = [.. Directory.GetFiles(resultsRoot, "Errors.*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static fileName => fileName is not null)
            .Select(static fileName => fileName!)
            .Order(StringComparer.Ordinal)];

        Assert.NotEmpty(featureErrorFiles);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullProductRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IProductRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ProductQueryHandlers_DoNotUseFullProductRepository() {
        string root = GetRepositoryRoot();
        string queryRoot = Path.Combine(root, "FoodDiary.Application", "Products", "Queries");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            queryRoot,
            "IProductRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ProductQueryHandlers_DoNotUseAggregateReadRepository() {
        string root = GetRepositoryRoot();
        string queryRoot = Path.Combine(root, "FoodDiary.Application", "Products", "Queries");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            queryRoot,
            "IProductReadRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ProductReadRepository_DoesNotExposeAggregatePaging() {
        string root = GetRepositoryRoot();
        string contractPath = Path.Combine(
            root,
            "FoodDiary.Application.Abstractions",
            "Products",
            "Common",
            "IProductReadRepository.cs");
        string source = File.ReadAllText(contractPath);

        Assert.DoesNotContain("GetPagedAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetByIdsWithUsageAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductQueryFilters", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductOverviewQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Products",
            "Queries",
            "GetProductsOverview",
            "GetProductsOverviewQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IProductOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product = FoodDiary.Domain.Entities.Products.Product", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductListQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Products",
            "Queries",
            "GetProducts",
            "GetProductsQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IProductOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product = FoodDiary.Domain.Entities.Products.Product", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductByIdQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Products",
            "Queries",
            "GetProductById",
            "GetProductByIdQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IProductOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product = FoodDiary.Domain.Entities.Products.Product", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductRecentQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Products",
            "Queries",
            "GetRecentProducts",
            "GetRecentProductsQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IRecentProductReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecentItemReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RecentProductUsage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product = FoodDiary.Domain.Entities.Products.Product", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UsdaProductSearchSuggestionProvider_UsesUsdaReadModelsInsteadOfFoodEntities() {
        string root = GetRepositoryRoot();
        string providerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Products",
            "SearchSuggestions",
            "UsdaProductSearchSuggestionProvider.cs");
        string source = File.ReadAllText(providerPath);

        Assert.Contains("SearchReadModelsAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiary.Domain.Entities.Usda", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".SearchAsync(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductCommandHandlersAndValidators_DoNotUseFullProductRepository() {
        string root = GetRepositoryRoot();
        string commandRoot = Path.Combine(root, "FoodDiary.Application", "Products", "Commands");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            commandRoot,
            "IProductRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullRecipeRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecipeRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void RecipeQueryHandlers_DoNotUseFullRecipeRepository() {
        string root = GetRepositoryRoot();
        string queryRoot = Path.Combine(root, "FoodDiary.Application", "Recipes", "Queries");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            queryRoot,
            "IRecipeRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void RecipeQueryHandlers_DoNotUseAggregateReadRepository() {
        string root = GetRepositoryRoot();
        string queryRoot = Path.Combine(root, "FoodDiary.Application", "Recipes", "Queries");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            queryRoot,
            "IRecipeReadRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void RecipeReadRepository_DoesNotExposeAggregatePaging() {
        string root = GetRepositoryRoot();
        string contractPath = Path.Combine(
            root,
            "FoodDiary.Application.Abstractions",
            "Recipes",
            "Common",
            "IRecipeReadRepository.cs");
        string source = File.ReadAllText(contractPath);

        Assert.DoesNotContain("GetPagedAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetByIdsWithUsageAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetExplorePagedAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RecipeQueryFilters", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeOverviewQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Recipes",
            "Queries",
            "GetRecipesOverview",
            "GetRecipesOverviewQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IRecipeOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecipeReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Recipe = FoodDiary.Domain.Entities.Recipes.Recipe", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeListQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Recipes",
            "Queries",
            "GetRecipes",
            "GetRecipesQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IRecipeOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecipeReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Recipe = FoodDiary.Domain.Entities.Recipes.Recipe", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeByIdQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Recipes",
            "Queries",
            "GetRecipeById",
            "GetRecipeByIdQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IRecipeOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecipeReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Recipe = FoodDiary.Domain.Entities.Recipes.Recipe", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeRecentQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Recipes",
            "Queries",
            "GetRecentRecipes",
            "GetRecentRecipesQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IRecentRecipeReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecipeReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecentItemReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RecentRecipeUsage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Recipe = FoodDiary.Domain.Entities.Recipes.Recipe", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeExploreQueryHandler_UsesDedicatedReadModelService() {
        string root = GetRepositoryRoot();
        string handlerPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Recipes",
            "Queries",
            "ExploreRecipes",
            "ExploreRecipesQueryHandler.cs");
        string source = File.ReadAllText(handlerPath);

        Assert.Contains("IRecipeOverviewReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IRecipeReadRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Recipe = FoodDiary.Domain.Entities.Recipes.Recipe", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeNutritionUpdater_DoesNotUseFullRecipeRepository() {
        string root = GetRepositoryRoot();
        string updaterPath = Path.Combine(root, "FoodDiary.Application", "Recipes", "Services", "RecipeNutritionUpdater.cs");

        string[] violations = FindReferencesInFiles(root, [updaterPath], "IRecipeRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void ProductAndRecipeCommands_DoNotCalculateUsageFromLoadedCollections() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] commandRoots = [
            Path.Combine(applicationRoot, "Products", "Commands"),
            Path.Combine(applicationRoot, "Recipes", "Commands"),
        ];
        string[] forbiddenPatterns = [
            "MealItems.Count",
            "RecipeIngredients.Count",
            "NestedRecipeUsages.Count",
        ];

        string[] violations = [.. commandRoots
            .SelectMany(SourceScanner.SourceFiles)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedRecipeCommandHandlersAndValidators_DoNotUseFullRecipeRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Recipes", "Commands", "CreateRecipe", "CreateRecipeCommandHandler.cs"),
            Path.Combine(applicationRoot, "Recipes", "Commands", "DeleteRecipe", "DeleteRecipeCommandHandler.cs"),
            Path.Combine(applicationRoot, "Recipes", "Commands", "DeleteRecipe", "DeleteRecipeCommandValidator.cs"),
            Path.Combine(applicationRoot, "Recipes", "Commands", "DuplicateRecipe", "DuplicateRecipeCommandHandler.cs"),
            Path.Combine(applicationRoot, "Recipes", "Commands", "UpdateRecipe", "UpdateRecipeCommandHandler.cs"),
            Path.Combine(applicationRoot, "Recipes", "Commands", "UpdateRecipe", "UpdateRecipeCommandValidator.cs"),
            Path.Combine(applicationRoot, "Recipes", "Commands", "UpdateRecipe", "UpdateRecipeValuePreparer.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "IRecipeRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void ProductAndRecipeInfrastructureAdapters_UseNarrowRepositoryContracts() {
        string root = GetRepositoryRoot();
        string infrastructureRoot = Path.Combine(root, "FoodDiary.Infrastructure");
        string[] migratedFiles = [
            Path.Combine(infrastructureRoot, "Services", "ProductLookupService.cs"),
            Path.Combine(infrastructureRoot, "Services", "RecipeLookupService.cs"),
            Path.Combine(infrastructureRoot, "Services", "RecipeAccessService.cs"),
            Path.Combine(infrastructureRoot, "Persistence", "Usda", "UsdaProductLinkRepository.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, migratedFiles, "IProductRepository"),
            .. FindReferencesInFiles(root, migratedFiles, "IRecipeRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullMealRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IMealRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void StatisticsQueries_UseDedicatedStatisticsReadServiceInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string statisticsRoot = Path.Combine(root, "FoodDiary.Application", "Statistics");
        string[] statisticsFiles = [.. SourceScanner.SourceFiles(statisticsRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, statisticsFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, statisticsFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, statisticsFiles, "GetByPeriodAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void TdeeQueries_UseStatisticsReadServiceInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string tdeeQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Tdee", "Queries");
        string[] tdeeQueryFiles = [.. SourceScanner.SourceFiles(tdeeQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, tdeeQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, tdeeQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, tdeeQueryFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, tdeeQueryFiles, "IWeightEntryReadRepository"),
            .. FindReferencesInFiles(root, tdeeQueryFiles, "IExerciseEntryReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void GamificationQueries_DoNotLoadMealAggregatesForWeeklyNutrition() {
        string root = GetRepositoryRoot();
        string gamificationQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Gamification", "Queries");
        string[] gamificationQueryFiles = [.. SourceScanner.SourceFiles(gamificationQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, gamificationQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, gamificationQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, gamificationQueryFiles, "GetByPeriodAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void CycleNutritionQueries_UseStatisticsReadServiceInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string cycleQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Cycles", "Queries");
        string[] cycleQueryFiles = [.. SourceScanner.SourceFiles(cycleQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, cycleQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, cycleQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, cycleQueryFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, cycleQueryFiles, "ICycleReadRepository"),
            .. FindReferencesInFiles(root, cycleQueryFiles, "GetByPeriodAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void WeeklyCheckInQueries_UseStatisticsReadServiceInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string weeklyCheckInQueriesRoot = Path.Combine(root, "FoodDiary.Application", "WeeklyCheckIn", "Queries");
        string[] weeklyCheckInQueryFiles = [.. SourceScanner.SourceFiles(weeklyCheckInQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "mealRepository.GetByPeriodAsync"),
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "IWeightEntryReadRepository"),
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "IWaistEntryReadRepository"),
            .. FindReferencesInFiles(root, weeklyCheckInQueryFiles, "IHydrationEntryReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ExportDiaryQuery_UsesDedicatedExportReadServiceInsteadOfMealRepository() {
        string root = GetRepositoryRoot();
        string exportDiaryQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Export", "Queries", "ExportDiary");
        string[] exportDiaryQueryFiles = [.. SourceScanner.SourceFiles(exportDiaryQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, exportDiaryQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, exportDiaryQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, exportDiaryQueryFiles, "GetByPeriodAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ExportCycleQuery_UsesDedicatedCycleReadServiceInsteadOfCycleRepository() {
        string root = GetRepositoryRoot();
        string exportCycleQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Export", "Queries", "ExportCycle");
        string[] exportCycleQueryFiles = [.. SourceScanner.SourceFiles(exportCycleQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, exportCycleQueryFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, exportCycleQueryFiles, "ICycleReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DailyMicronutrientsQuery_UsesDedicatedReadServiceInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string dailyMicronutrientsQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Usda", "Queries", "GetDailyMicronutrients");
        string[] dailyMicronutrientsQueryFiles = [.. SourceScanner.SourceFiles(dailyMicronutrientsQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, dailyMicronutrientsQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, dailyMicronutrientsQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, dailyMicronutrientsQueryFiles, "FoodDiary.Domain.Entities.Products"),
            .. FindReferencesInFiles(root, dailyMicronutrientsQueryFiles, "GetWithItemsAndProductsAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DailyMicronutrientReadService_UsesMealProductReadModelsInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Usda",
            "Services",
            "UsdaDailyMicronutrientReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Products"),
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Usda"),
            .. FindReferencesInFiles(root, serviceFiles, "GetWithItemsAndProductsAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetNutrientsByFdcIdsAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetDailyReferenceValuesAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void UsdaFoodQueries_UseDedicatedReadServiceInsteadOfFoodRepository() {
        string root = GetRepositoryRoot();
        string usdaQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Usda", "Queries");
        string[] usdaFoodQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(usdaQueriesRoot, "SearchUsdaFoods")),
            .. SourceScanner.SourceFiles(Path.Combine(usdaQueriesRoot, "GetMicronutrients")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, usdaFoodQueryFiles, "IUsdaFoodReadRepository"),
            .. FindReferencesInFiles(root, usdaFoodQueryFiles, "FoodDiary.Domain.Entities.Usda"),
            .. FindReferencesInFiles(root, usdaFoodQueryFiles, "FoodDiary.Domain.ValueObjects"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void UsdaFoodReadService_UsesReadModelsInsteadOfUsdaAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Usda",
            "Services",
            "UsdaFoodReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Usda"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.SearchAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetByFdcIdAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetNutrientsAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetPortionsAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ConsumptionQueries_UseDedicatedReadServiceInsteadOfMealRepository() {
        string root = GetRepositoryRoot();
        string consumptionQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Consumptions", "Queries");
        string[] consumptionQueryFiles = [.. SourceScanner.SourceFiles(consumptionQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, consumptionQueryFiles, "IMealReadRepository"),
            .. FindReferencesInFiles(root, consumptionQueryFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, consumptionQueryFiles, "mealRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ConsumptionReadService_UsesReadModelsInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Consumptions",
            "Services",
            "ConsumptionReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.FavoriteMeals"),
            .. FindReferencesInFiles(root, serviceFiles, "mealRepository.GetPagedAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "mealRepository.GetByIdAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "favoriteMealRepository.GetByMealIdsAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminContentReadContracts_DoNotFallbackToAggregateDefaultReadModels() {
        string root = GetRepositoryRoot();
        string[] contractFiles = [
            Path.Combine(root, "FoodDiary.Application.Abstractions", "Lessons", "Common", "INutritionLessonReadRepository.cs"),
            Path.Combine(root, "FoodDiary.Application.Abstractions", "Admin", "Common", "IEmailTemplateReadRepository.cs"),
            Path.Combine(root, "FoodDiary.Application.Abstractions", "ContentReports", "Common", "IContentReportReadRepository.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, contractFiles, "async Task<IReadOnlyList<LessonSummaryReadModel>>"),
            .. FindReferencesInFiles(root, contractFiles, "async Task<IReadOnlyList<LessonAdminReadModel>>"),
            .. FindReferencesInFiles(root, contractFiles, "async Task<LessonDetailReadModel?>"),
            .. FindReferencesInFiles(root, contractFiles, "async Task<IReadOnlyList<EmailTemplateReadModel>>"),
            .. FindReferencesInFiles(root, contractFiles, "async Task<(IReadOnlyList<ContentReportAdminReadModel>"),
            .. FindReferencesInFiles(root, contractFiles, "private static"),
            .. FindReferencesInFiles(root, contractFiles, ".Select(To"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ConsumptionMappings_DoNotExposePagedMealAggregateReadModels() {
        string root = GetRepositoryRoot();
        string mappingPath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Consumptions",
            "Mappings",
            "ConsumptionMappings.cs");
        string[] mappingFiles = [mappingPath];

        string[] violations = [
            .. FindReferencesInFiles(root, mappingFiles, "IReadOnlyList<Meal>"),
            .. FindReferencesInFiles(root, mappingFiles, "(IReadOnlyList<Meal> Items, int TotalItems)"),
            .. FindReferencesInFiles(root, mappingFiles, "PagedResponse<ConsumptionModel>"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminQueries_UseAdminUserReadServiceModelsInsteadOfUserAggregates() {
        string root = GetRepositoryRoot();
        string adminQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Admin", "Queries");
        string[] adminQueryFiles = [.. SourceScanner.SourceFiles(adminQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, adminQueryFiles, "FoodDiary.Domain.Entities.Users"),
            .. FindReferencesInFiles(root, adminQueryFiles, "Domain.Entities.Users.User"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminBillingQueries_UseReadServiceInsteadOfBillingRepository() {
        string root = GetRepositoryRoot();
        string adminQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Admin", "Queries");
        string[] adminBillingQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminBillingPayments")),
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminBillingSubscriptions")),
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminBillingWebhookEvents")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, adminBillingQueryFiles, "IAdminBillingReadRepository"),
            .. FindReferencesInFiles(root, adminBillingQueryFiles, "FoodDiary.Domain.Entities.Billing"),
            .. FindReferencesInFiles(root, adminBillingQueryFiles, "AdminBillingQueryFilters"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminAuditAndLoginQueries_UseReadServicesInsteadOfRepositories() {
        string root = GetRepositoryRoot();
        string adminQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Admin", "Queries");
        string[] adminAuditAndLoginQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminUserLoginEvents")),
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminUserLoginSummary")),
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminUserRoleAudit")),
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminImpersonationSessions")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, adminAuditAndLoginQueryFiles, "IUserLoginEventReadRepository"),
            .. FindReferencesInFiles(root, adminAuditAndLoginQueryFiles, "IAdminUserRoleAuditReadRepository"),
            .. FindReferencesInFiles(root, adminAuditAndLoginQueryFiles, "IAdminImpersonationSessionReadRepository"),
            .. FindReferencesInFiles(root, adminAuditAndLoginQueryFiles, "MaskIpAddress"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminSummaryQueries_UseReadServicesInsteadOfRepositories() {
        string root = GetRepositoryRoot();
        string adminQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Admin", "Queries");
        string[] adminSummaryQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminAiUsageSummary")),
            .. SourceScanner.SourceFiles(Path.Combine(adminQueriesRoot, "GetAdminDashboardSummary")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, adminSummaryQueryFiles, "IAiUsageReadRepository"),
            .. FindReferencesInFiles(root, adminSummaryQueryFiles, "IContentReportReadRepository"),
            .. FindReferencesInFiles(root, adminSummaryQueryFiles, "FoodDiary.Domain.Enums"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminUserReadService_UsesReadModelsInsteadOfUserAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Admin",
            "Services",
            "AdminUserReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Users"),
            .. FindReferencesInFiles(root, serviceFiles, "userAdminReadRepository.GetPagedAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "userAdminReadRepository.GetAdminDashboardSummaryAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminContentReadService_UsesAiPromptReadModelsInsteadOfPromptAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Admin",
            "Services",
            "AdminContentReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Ai"),
            .. FindReferencesInFiles(root, serviceFiles, "AiPromptTemplate>"),
            .. FindReferencesInFiles(root, serviceFiles, "aiPromptTemplateRepository.GetAllAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminContentReadService_UsesEmailTemplateReadModelsInsteadOfTemplateAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Admin",
            "Services",
            "AdminContentReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "EmailTemplate>"),
            .. FindReferencesInFiles(root, serviceFiles, "emailTemplateRepository.GetAllAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminContentReadService_UsesLessonAndReportReadModelsInsteadOfAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Admin",
            "Services",
            "AdminContentReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Content"),
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Social"),
            .. FindReferencesInFiles(root, serviceFiles, "NutritionLesson>"),
            .. FindReferencesInFiles(root, serviceFiles, "ContentReport>"),
            .. FindReferencesInFiles(root, serviceFiles, "lessonRepository.GetAllAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "contentReportRepository.GetPagedAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void CycleReadService_UsesCycleReadModelsInsteadOfProfileAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Cycles",
            "Services",
            "CycleReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, serviceFiles, "cycleRepository.GetCurrentAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void FastingReadServices_UseFastingReadModelsInsteadOfOccurrenceAggregates() {
        string root = GetRepositoryRoot();
        string[] serviceFiles = [
            Path.Combine(root, "FoodDiary.Application", "Fasting", "Services", "FastingReadService.cs"),
            Path.Combine(root, "FoodDiary.Application", "Fasting", "Services", "FastingAnalyticsService.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Tracking.Fasting"),
            .. FindReferencesInFiles(root, serviceFiles, "FastingOccurrence>"),
            .. FindReferencesInFiles(root, serviceFiles, "FastingCheckIn>"),
            .. FindReferencesInFiles(root, serviceFiles, "fastingOccurrenceRepository.GetCurrentAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "fastingOccurrenceRepository.GetByUserAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "fastingOccurrenceRepository.GetPagedByUserAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "fastingCheckInRepository.GetByOccurrenceIdsAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ExportDiaryReadAndGenerationServices_UseMealReadModelsInsteadOfMealAggregates() {
        string root = GetRepositoryRoot();
        string[] serviceFiles = [
            Path.Combine(root, "FoodDiary.Application", "Export", "Services", "ExportDiaryReadService.cs"),
            Path.Combine(root, "FoodDiary.Application", "Export", "Services", "DiaryCsvGenerator.cs"),
            Path.Combine(root, "FoodDiary.Application.Abstractions", "Export", "Common", "IDiaryPdfGenerator.cs"),
            Path.Combine(root, "FoodDiary.Application.Abstractions", "Export", "Models", "ExportDiaryMealsReadModel.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Meals"),
            .. FindReferencesInFiles(root, serviceFiles, "IReadOnlyList<Meal>"),
            .. FindReferencesInFiles(root, serviceFiles, "mealRepository.GetByPeriodAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void UserQueries_UseProfileReadServiceModelsInsteadOfUserAggregates() {
        string root = GetRepositoryRoot();
        string userQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Users", "Queries");
        string[] userQueryFiles = [.. SourceScanner.SourceFiles(userQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, userQueryFiles, "FoodDiary.Domain.Entities.Users"),
            .. FindReferencesInFiles(root, userQueryFiles, "FoodDiary.Domain.Entities.Notifications"),
            .. FindReferencesInFiles(root, userQueryFiles, "FoodDiary.Domain.Entities.Dietologist"),
            .. FindReferencesInFiles(root, userQueryFiles, "IWebPushSubscriptionReadRepository"),
            .. FindReferencesInFiles(root, userQueryFiles, "IDietologistInvitationReadRepository"),
            .. FindReferencesInFiles(root, userQueryFiles, "GetAccessibleUserAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ProfileOverviewReadService_UsesDietologistReadModelsInsteadOfInvitationAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Users",
            "Services",
            "ProfileOverviewReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Dietologist"),
            .. FindReferencesInFiles(root, serviceFiles, "GetActiveByClientAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByClientAndStatusAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ProfileOverviewReadService_UsesWebPushReadModelsInsteadOfSubscriptionAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Users",
            "Services",
            "ProfileOverviewReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Notifications"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByUserAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DietologistQueries_UseReadServicesInsteadOfRepositories() {
        string root = GetRepositoryRoot();
        string dietologistQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Dietologist", "Queries");
        string[] dietologistQueryFiles = [.. SourceScanner.SourceFiles(dietologistQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, dietologistQueryFiles, "IDietologistInvitationReadRepository"),
            .. FindReferencesInFiles(root, dietologistQueryFiles, "IRecommendationReadRepository"),
            .. FindReferencesInFiles(root, dietologistQueryFiles, "FoodDiary.Domain.Entities.Dietologist"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DietologistReadServices_UseReadModelsInsteadOfDietologistAggregates() {
        string root = GetRepositoryRoot();
        string dietologistServicesRoot = Path.Combine(root, "FoodDiary.Application", "Dietologist", "Services");
        string[] readServiceFiles = [
            Path.Combine(dietologistServicesRoot, "DietologistInvitationReadService.cs"),
            Path.Combine(dietologistServicesRoot, "DietologistClientReadService.cs"),
            Path.Combine(dietologistServicesRoot, "DietologistRecommendationReadService.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, readServiceFiles, "FoodDiary.Domain.Entities.Dietologist"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetByIdAsync"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetByClientAndStatusAsync"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetActiveByClientAsync"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetActiveByClientAndDietologistAsync"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetActiveByDietologistAsync"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetByClientAsync"),
            .. FindReferencesInFiles(root, readServiceFiles, "GetByDietologistAndClientAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingQueries_UseBillingProfileModelsInsteadOfUserAggregates() {
        string root = GetRepositoryRoot();
        string billingQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Billing", "Queries");
        string[] billingQueryFiles = [.. SourceScanner.SourceFiles(billingQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, billingQueryFiles, "FoodDiary.Domain.Entities.Users"),
            .. FindReferencesInFiles(root, billingQueryFiles, "FoodDiary.Domain.Entities.Billing"),
            .. FindReferencesInFiles(root, billingQueryFiles, "IBillingSubscriptionReadRepository"),
            .. FindReferencesInFiles(root, billingQueryFiles, "GetAccessibleUserAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DietologistQueries_UseUserReadModelsInsteadOfUserAggregates() {
        string root = GetRepositoryRoot();
        string dietologistQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Dietologist", "Queries");
        string[] dietologistQueryFiles = [.. SourceScanner.SourceFiles(dietologistQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, dietologistQueryFiles, "FoodDiary.Domain.Entities.Users"),
            .. FindReferencesInFiles(root, dietologistQueryFiles, "GetAccessibleUserAsync"),
            .. FindReferencesInFiles(root, dietologistQueryFiles, "GetUserByIdAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void FavoriteStatusQueries_UseExistenceReadsInsteadOfFavoriteAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] favoriteStatusQueryFiles = [
            Path.Combine(applicationRoot, "FavoriteMeals", "Queries", "IsMealFavorite", "IsMealFavoriteQueryHandler.cs"),
            Path.Combine(applicationRoot, "FavoriteProducts", "Queries", "IsProductFavorite", "IsProductFavoriteQueryHandler.cs"),
            Path.Combine(applicationRoot, "FavoriteRecipes", "Queries", "IsRecipeFavorite", "IsRecipeFavoriteQueryHandler.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, favoriteStatusQueryFiles, "FoodDiary.Domain.Entities.Favorite"),
            .. FindReferencesInFiles(root, favoriteStatusQueryFiles, "GetByMealIdAsync"),
            .. FindReferencesInFiles(root, favoriteStatusQueryFiles, "GetByProductIdAsync"),
            .. FindReferencesInFiles(root, favoriteStatusQueryFiles, "GetByRecipeIdAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void FavoriteQueries_UseReadServicesInsteadOfFavoriteRepositoriesAndAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] favoriteQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "FavoriteMeals", "Queries")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "FavoriteProducts", "Queries")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "FavoriteRecipes", "Queries")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, favoriteQueryFiles, "FoodDiary.Domain.Entities.Favorite"),
            .. FindReferencesInFiles(root, favoriteQueryFiles, "IFavoriteMealReadRepository"),
            .. FindReferencesInFiles(root, favoriteQueryFiles, "IFavoriteProductReadRepository"),
            .. FindReferencesInFiles(root, favoriteQueryFiles, "IFavoriteRecipeReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void FavoriteReadServices_UseReadModelsInsteadOfFavoriteAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] serviceFiles = [
            Path.Combine(applicationRoot, "FavoriteMeals", "Services", "FavoriteMealReadService.cs"),
            Path.Combine(applicationRoot, "FavoriteProducts", "Services", "FavoriteProductReadService.cs"),
            Path.Combine(applicationRoot, "FavoriteRecipes", "Services", "FavoriteRecipeReadService.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Favorite"),
            .. FindReferencesInFiles(root, serviceFiles, "GetAllAsync(userId"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationQueries_UseReadServicesInsteadOfNotificationAggregates() {
        string root = GetRepositoryRoot();
        string notificationQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Notifications", "Queries");
        string[] notificationQueryFiles = [.. SourceScanner.SourceFiles(notificationQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, notificationQueryFiles, "FoodDiary.Domain.Entities.Notifications"),
            .. FindReferencesInFiles(root, notificationQueryFiles, "INotificationReadRepository"),
            .. FindReferencesInFiles(root, notificationQueryFiles, "IWebPushSubscriptionReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void WebPushSubscriptionReadService_UsesReadModelsInsteadOfSubscriptionAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Notifications",
            "Services",
            "WebPushSubscriptionReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Notifications"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByUserAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationFeedReadService_UsesReadModelsInsteadOfNotificationAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Notifications",
            "Services",
            "NotificationFeedReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Notifications"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByUserAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void WearableQueries_UseReadServicesInsteadOfWearableAggregates() {
        string root = GetRepositoryRoot();
        string wearableQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Wearables", "Queries");
        string[] wearableQueryFiles = [.. SourceScanner.SourceFiles(wearableQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, wearableQueryFiles, "FoodDiary.Domain.Entities.Wearables"),
            .. FindReferencesInFiles(root, wearableQueryFiles, "IWearableConnectionReadRepository"),
            .. FindReferencesInFiles(root, wearableQueryFiles, "IWearableSyncReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void WearableReadService_UsesReadModelsInsteadOfWearableAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Wearables",
            "Services",
            "WearableReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Wearables"),
            .. FindReferencesInFiles(root, serviceFiles, "GetAllForUserAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetDailySummaryAsync(userId"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void TrackingEntryQueries_UseReadServicesInsteadOfTrackingAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        (string Slice, string[] RepositoryNames)[] slices = [
            ("Exercises", ["IExerciseEntryReadRepository"]),
            ("Hydration", ["IHydrationEntryReadRepository"]),
            ("WaistEntries", ["IWaistEntryReadRepository"]),
            ("WeightEntries", ["IWeightEntryReadRepository"]),
        ];

        string[] violations = [.. slices.SelectMany(slice => {
            string queriesRoot = Path.Combine(applicationRoot, slice.Slice, "Queries");
            string[] queryFiles = [.. SourceScanner.SourceFiles(queriesRoot)];

            return new[] {
                FindReferencesInFiles(root, queryFiles, "FoodDiary.Domain.Entities.Tracking"),
                [.. slice.RepositoryNames.SelectMany(repositoryName => FindReferencesInFiles(root, queryFiles, repositoryName))],
            }.SelectMany(items => items);
        })];

        Assert.Empty(violations);
    }

    [Fact]
    public void ExerciseEntryReadService_UsesReadModelsInsteadOfTrackingAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Exercises",
            "Services",
            "ExerciseEntryReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByDateRangeAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DashboardBodyReadService_UsesReadModelsInsteadOfTrackingAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Dashboard",
            "Services",
            "RepositoryDashboardBodyReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, serviceFiles, "GetEntriesAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByPeriodAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void TrackingEntryReadServices_UseReadModelsInsteadOfTrackingAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] serviceFiles = [
            Path.Combine(applicationRoot, "WeightEntries", "Services", "WeightEntryReadService.cs"),
            Path.Combine(applicationRoot, "WaistEntries", "Services", "WaistEntryReadService.cs"),
            Path.Combine(applicationRoot, "Hydration", "Services", "HydrationEntryReadService.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByPeriodAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByDateAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DashboardSnapshotServices_UseReadModelsInsteadOfDashboardAggregates() {
        string root = GetRepositoryRoot();
        string dashboardServicesRoot = Path.Combine(root, "FoodDiary.Application", "Dashboard", "Services");
        string[] serviceFiles = [
            Path.Combine(dashboardServicesRoot, "DashboardSnapshotBuilder.cs"),
            Path.Combine(dashboardServicesRoot, "DashboardSectionDataLoader.cs"),
            Path.Combine(dashboardServicesRoot, "IDashboardSectionDataLoader.cs"),
            Path.Combine(dashboardServicesRoot, "DashboardBuildContext.cs"),
            Path.Combine(dashboardServicesRoot, "DashboardMapping.cs"),
            Path.Combine(dashboardServicesRoot, "DashboardBodyMapper.cs"),
            Path.Combine(dashboardServicesRoot, "DashboardStatisticsMapper.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Tracking"),
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Users"),
            .. FindReferencesInFiles(root, serviceFiles, "FastingOccurrence"),
            .. FindReferencesInFiles(root, serviceFiles, "IFastingOccurrenceReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void FastingQueries_UseReadServicesInsteadOfFastingAggregates() {
        string root = GetRepositoryRoot();
        string fastingQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Fasting", "Queries");
        string[] fastingQueryFiles = [.. SourceScanner.SourceFiles(fastingQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, fastingQueryFiles, "FoodDiary.Domain.Entities.Tracking.Fasting"),
            .. FindReferencesInFiles(root, fastingQueryFiles, "IFastingOccurrenceReadRepository"),
            .. FindReferencesInFiles(root, fastingQueryFiles, "IFastingCheckInReadRepository"),
            .. FindReferencesInFiles(root, fastingQueryFiles, "IFastingTelemetryEventReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ShoppingListQueries_UseReadServicesInsteadOfShoppingAggregates() {
        string root = GetRepositoryRoot();
        string shoppingListQueriesRoot = Path.Combine(root, "FoodDiary.Application", "ShoppingLists", "Queries");
        string[] shoppingListQueryFiles = [.. SourceScanner.SourceFiles(shoppingListQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, shoppingListQueryFiles, "FoodDiary.Domain.Entities.Shopping"),
            .. FindReferencesInFiles(root, shoppingListQueryFiles, "IShoppingListReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void MealPlanQueries_UseReadServicesInsteadOfMealPlanAggregates() {
        string root = GetRepositoryRoot();
        string mealPlanQueriesRoot = Path.Combine(root, "FoodDiary.Application", "MealPlans", "Queries");
        string[] mealPlanQueryFiles = [.. SourceScanner.SourceFiles(mealPlanQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, mealPlanQueryFiles, "FoodDiary.Domain.Entities.MealPlans"),
            .. FindReferencesInFiles(root, mealPlanQueryFiles, "IMealPlanReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ShoppingListReadService_UsesReadModelsInsteadOfShoppingAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "ShoppingLists",
            "Services",
            "ShoppingListReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Shopping"),
            .. FindReferencesInFiles(root, serviceFiles, "shoppingListRepository.GetAllAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "shoppingListRepository.GetByIdAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "shoppingListRepository.GetCurrentAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void MealPlanReadService_UsesReadModelsInsteadOfMealPlanAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "MealPlans",
            "Services",
            "MealPlanReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.MealPlans"),
            .. FindReferencesInFiles(root, serviceFiles, "mealPlanRepository.GetCuratedAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "mealPlanRepository.GetByUserAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "mealPlanRepository.GetByIdAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void SocialQueries_UseReadServicesInsteadOfSocialAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] socialQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "RecipeLikes", "Queries")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "RecipeComments", "Queries")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, socialQueryFiles, "FoodDiary.Domain.Entities.Social"),
            .. FindReferencesInFiles(root, socialQueryFiles, "Domain.Entities.Recipes.RecipeComment"),
            .. FindReferencesInFiles(root, socialQueryFiles, "IRecipeLikeReadRepository"),
            .. FindReferencesInFiles(root, socialQueryFiles, "IRecipeCommentReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void SocialReadServices_UseReadModelsInsteadOfSocialAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] serviceFiles = [
            Path.Combine(applicationRoot, "RecipeLikes", "Services", "RecipeLikeReadService.cs"),
            Path.Combine(applicationRoot, "RecipeComments", "Services", "RecipeCommentReadService.cs"),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Social"),
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Recipes"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByUserAndRecipeAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "GetPagedByRecipeAsync(recipeId"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void AiQueries_UseReadServicesInsteadOfUsageRepositories() {
        string root = GetRepositoryRoot();
        string aiQueriesRoot = Path.Combine(root, "FoodDiary.Application", "Ai", "Queries");
        string[] aiQueryFiles = [.. SourceScanner.SourceFiles(aiQueriesRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, aiQueryFiles, "IAiUsageReadRepository"),
            .. FindReferencesInFiles(root, aiQueryFiles, "IAiUsageRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ContentQueries_UseReadServicesInsteadOfContentAggregates() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] contentQueryFiles = [
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "Lessons", "Queries")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "DailyAdvices", "Queries")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminLessons")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminEmailTemplates")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminAiPrompts")),
            .. SourceScanner.SourceFiles(Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminContentReports")),
        ];

        string[] violations = [
            .. FindReferencesInFiles(root, contentQueryFiles, "FoodDiary.Domain.Entities.Content"),
            .. FindReferencesInFiles(root, contentQueryFiles, "FoodDiary.Domain.Entities.Ai"),
            .. FindReferencesInFiles(root, contentQueryFiles, "FoodDiary.Domain.Entities.Social"),
            .. FindReferencesInFiles(root, contentQueryFiles, "INutritionLessonReadRepository"),
            .. FindReferencesInFiles(root, contentQueryFiles, "IDailyAdviceReadRepository"),
            .. FindReferencesInFiles(root, contentQueryFiles, "IEmailTemplateReadRepository"),
            .. FindReferencesInFiles(root, contentQueryFiles, "IAiPromptTemplateReadRepository"),
            .. FindReferencesInFiles(root, contentQueryFiles, "IContentReportReadRepository"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void DailyAdviceReadService_UsesReadModelsInsteadOfContentAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "DailyAdvices",
            "Services",
            "DailyAdviceReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Content"),
            .. FindReferencesInFiles(root, serviceFiles, "GetByLocaleAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void LessonReadService_UsesReadModelsInsteadOfContentAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Lessons",
            "Services",
            "LessonReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Content"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetByLocaleAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetByIdAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetUserProgressAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "repository.GetUserProgressForLessonAsync"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingOverviewReadService_UsesReadModelsInsteadOfBillingAggregates() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Billing",
            "Services",
            "BillingOverviewReadService.cs");
        string[] serviceFiles = [servicePath];

        string[] violations = [
            .. FindReferencesInFiles(root, serviceFiles, "FoodDiary.Domain.Entities.Billing"),
            .. FindReferencesInFiles(root, serviceFiles, "billingSubscriptionRepository.GetByUserIdAsync"),
            .. FindReferencesInFiles(root, serviceFiles, "BillingSubscription?"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullNutritionLessonRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "INutritionLessonRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullNotificationRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "INotificationRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullFastingRepositories() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IFastingPlanRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IFastingOccurrenceRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IFastingCheckInRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IFastingSessionRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IFastingTelemetryEventRepository", []),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullBillingSubscriptionRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IBillingSubscriptionRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullAuthenticationBillingAndWearableSupportRepositories() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IUserLoginEventRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IRefreshTokenSessionRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IBillingPaymentRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IBillingWebhookEventRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IWearableConnectionRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IWearableSyncRepository", []),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullExerciseEntryRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IExerciseEntryRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullWeightEntryRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IWeightEntryRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullWaistEntryRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IWaistEntryRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullHydrationEntryRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IHydrationEntryRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullShoppingListRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IShoppingListRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullMealPlanRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IMealPlanRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullRecipeCommentRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecipeCommentRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullRecipeLikeRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecipeLikeRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullFavoriteProductRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IFavoriteProductRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullFavoriteRecipeRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IFavoriteRecipeRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullFavoriteMealRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IFavoriteMealRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullRecentItemRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecentItemRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullDietologistInvitationRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IDietologistInvitationRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullRecommendationRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IRecommendationRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullImageAssetRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IImageAssetRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullWebPushSubscriptionRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "IWebPushSubscriptionRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullAiAndContentSupportRepositories() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IAiUsageRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IAiPromptTemplateRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IEmailTemplateRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IContentReportRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IDailyAdviceRepository", []),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullAdminUsdaAndOpenFoodFactsSupportRepositories() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = [
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IAdminBillingRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IAdminImpersonationSessionRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IAdminUserRoleAuditRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IUsdaFoodRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IUsdaProductLinkRepository", []),
            .. FindRepositoryReferenceViolations(root, applicationRoot, "IOpenFoodFactsProductCacheRepository", []),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseFullCycleRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = FindRepositoryReferenceViolations(
            root,
            applicationRoot,
            "ICycleRepository",
            []);

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAuthenticationMutationServices_DoNotUseCurrentUserAccessPolicy() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Authentication", "Commands", "ConfirmPasswordReset", "ConfirmPasswordResetCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "GoogleLogin", "GoogleLoginCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "LinkTelegram", "LinkTelegramCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "RequestPasswordReset", "RequestPasswordResetCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "ResendEmailVerification", "ResendEmailVerificationCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "RestoreAccount", "RestoreAccountCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "VerifyEmail", "VerifyEmailCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Services", "AuthenticationTokenService.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "CurrentUserAccessPolicy");

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseLegacyCurrentUserAccessLoader() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, ["CurrentUserAccessLoader"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void UsersSlice_DoesNotRegrowStandaloneCurrentUserAccessService() {
        string root = GetRepositoryRoot();
        string usersCommonRoot = Path.Combine(root, "FoodDiary.Application", "Users", "Common");
        string servicePath = Path.Combine(usersCommonRoot, "CurrentUserAccessService.cs");

        Assert.False(File.Exists(servicePath), "UserContextService should remain the single current-user access implementation.");
    }

    [Fact]
    public void DietologistUserContextService_UsesSharedUserContextAndNarrowLookupServices() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Dietologist",
            "Services",
            "DietologistUserContextService.cs");
        string source = File.ReadAllText(servicePath);

        Assert.Contains("IUserContextService", source, StringComparison.Ordinal);
        Assert.Contains("IDietologistUserLookupService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUserRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentUserAccessPolicy", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BillingSlice_UsesCurrentUserAccessPolicyOnlyThroughBillingUserLookupService() {
        string root = GetRepositoryRoot();
        string billingRoot = Path.Combine(root, "FoodDiary.Application", "Billing");
        string allowedPath = Path.Combine(billingRoot, "Services", "BillingUserLookupService.cs");
        string[] billingFiles = [.. SourceScanner.SourceFiles(billingRoot)
            .Where(path => !string.Equals(path, allowedPath, StringComparison.OrdinalIgnoreCase))];

        string[] violations = FindReferencesInFiles(root, billingFiles, "CurrentUserAccessPolicy");

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationsSlice_DoesNotUseCurrentUserAccessPolicyDirectly() {
        string root = GetRepositoryRoot();
        string notificationsRoot = Path.Combine(root, "FoodDiary.Application", "Notifications");
        string[] notificationFiles = [.. SourceScanner.SourceFiles(notificationsRoot)];

        string[] violations = FindReferencesInFiles(root, notificationFiles, "CurrentUserAccessPolicy");

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingUserContextService_DelegatesUserAccessToFocusedServices() {
        string root = GetRepositoryRoot();
        string servicePath = Path.Combine(
            root,
            "FoodDiary.Application",
            "Billing",
            "Services",
            "BillingUserContextService.cs");
        string source = File.ReadAllText(servicePath);

        Assert.Contains("IUserContextService", source, StringComparison.Ordinal);
        Assert.Contains("IBillingUserLookupService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUserRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentUserAccessPolicy", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UserProfileFeatureSlices_UseCurrentUserAccessPolicyOnlyThroughDedicatedProfileServices() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        (string Slice, string AllowedRelativePath)[] slices = [
            ("Ai", Path.Combine("Services", "AiUserContextService.cs")),
            ("Dashboard", Path.Combine("Services", "DashboardUserContextService.cs")),
            ("Gamification", Path.Combine("Services", "GamificationUserProfileService.cs")),
            ("Hydration", Path.Combine("Services", "HydrationGoalService.cs")),
            ("Tdee", Path.Combine("Services", "TdeeUserProfileService.cs")),
            ("WeeklyCheckIn", Path.Combine("Services", "WeeklyCheckInUserProfileService.cs")),
        ];

        string[] violations = [.. slices.SelectMany(slice => {
            string sliceRoot = Path.Combine(applicationRoot, slice.Slice);
            string allowedPath = Path.Combine(sliceRoot, slice.AllowedRelativePath);
            string[] files = [.. SourceScanner.SourceFiles(sliceRoot)
                .Where(path => !string.Equals(path, allowedPath, StringComparison.OrdinalIgnoreCase))];

            return FindReferencesInFiles(root, files, "CurrentUserAccessPolicy");
        })];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedUserHandlers_DoNotUseCurrentUserAccessPolicyDirectly() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Users", "Commands", "AcceptAiConsent", "AcceptAiConsentCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "ChangePassword", "ChangePasswordCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "DeleteUser", "DeleteUserCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "RevokeAiConsent", "RevokeAiConsentCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "SetPassword", "SetPasswordCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "UpdateDesiredWaist", "UpdateDesiredWaistCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "UpdateDesiredWeight", "UpdateDesiredWeightCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "UpdateGoals", "UpdateGoalsCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "UpdateUser", "UpdateUserCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Commands", "UpdateUserAppearance", "UpdateUserAppearanceCommandHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Queries", "GetDesiredWaist", "GetDesiredWaistQueryHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Queries", "GetDesiredWeight", "GetDesiredWeightQueryHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Queries", "GetProfileOverview", "GetProfileOverviewQueryHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Queries", "GetUserById", "GetUserByIdQueryHandler.cs"),
            Path.Combine(applicationRoot, "Users", "Queries", "GetUserGoals", "GetUserGoalsQueryHandler.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "CurrentUserAccessPolicy");

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructurePersistenceRoot_StayLimitedToSharedEfInfrastructureFiles() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Infrastructure", "Persistence");
        string[] allowedFiles = [
            "EfUnitOfWork.cs",
            "FoodDiaryDbContext.cs",
            "FoodDiaryDbContextFactory.cs",
        ];

        string?[] actualFiles = [.. Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)];
        string?[] unexpectedFiles = [.. actualFiles
            .Where(fileName =>
                fileName is not null &&
                !allowedFiles.Contains(fileName, StringComparer.Ordinal) &&
                !fileName.StartsWith("FoodDiaryDbContext.", StringComparison.Ordinal))];

        Assert.Empty(unexpectedFiles);
    }

    [Fact]
    public void DashboardRuntimeReadPath_UsesDedicatedInfrastructureReadService() {
        string root = GetRepositoryRoot();
        string dashboardPlanPath = Path.Combine(root, "FoodDiary.Application", "Dashboard", "Dashboard-Query-Plan.md");
        Assert.False(File.Exists(dashboardPlanPath), "Dashboard migration plan should not be kept after the dedicated read path is implemented.");

        string repositoryRegistrationPath = Path.Combine(root, "FoodDiary.Infrastructure", "DependencyInjection.Dashboard.cs");
        string registrationSource = File.ReadAllText(repositoryRegistrationPath);
        Assert.Contains("services.RemoveAll<IDashboardReadService>();", registrationSource, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<IDashboardReadService, DashboardReadService>();", registrationSource, StringComparison.Ordinal);

        string applicationRegistrationPath = Path.Combine(root, "FoodDiary.Application", "DependencyInjection.cs");
        string applicationRegistrationSource = File.ReadAllText(applicationRegistrationPath);
        Assert.Contains("services.TryAddScoped<IDashboardReadService, ComposedDashboardReadService>();", applicationRegistrationSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotReferencePresentationOrAspNetTransportTypes() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] forbiddenPatterns = [
            "FoodDiary.Presentation.Api",
            "Microsoft.AspNetCore",
            "IActionResult",
            "ControllerBase",
            "HttpContext",
            "HttpRequest",
            "HttpResponse",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(applicationRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainEventHandlers_DoNotDependOnDirectExternalSideEffectDispatchers() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] forbiddenParameterTypes = [
            "IEmailSender",
            "IDietologistEmailSender",
            "IEmailTransport",
            "IWebPushNotificationSender",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .SelectMany(path => ReadDomainEventHandlerConstructorParameters(path)
                .Where(parameter => forbiddenParameterTypes.Contains(parameter.TypeName, StringComparer.Ordinal))
                .Select(parameter => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(root, parameter.Path)}:{parameter.Line} {parameter.ClassName} depends on {parameter.TypeName}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainEventHandlers_DoNotInvokeExternalSideEffectsDirectly() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] forbiddenPatterns = [
            ".SendAsync(",
            ".PushUnreadCountAsync(",
            ".PushNotificationsChangedAsync(",
            ".UploadAsync(",
            ".DeleteAsync(",
            ".ChargeAsync(",
            ".CreateCheckout",
            ".CreateCustomerPortal",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(IsDomainEventHandlerSourceFile)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void BusinessEmailSenders_UseRequiredOutboxInsteadOfDirectTransportFallback() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string emailSenderPath = Path.Combine(applicationRoot, "Authentication", "Services", "EmailSender.cs");
        string dietologistEmailSenderPath = Path.Combine(applicationRoot, "Dietologist", "Services", "DietologistEmailSender.cs");

        string emailSenderSource = File.ReadAllText(emailSenderPath);
        string dietologistEmailSenderSource = File.ReadAllText(dietologistEmailSenderPath);

        Assert.Contains("IEmailOutbox emailOutbox", emailSenderSource, StringComparison.Ordinal);
        Assert.Contains("IEmailOutbox emailOutbox", dietologistEmailSenderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("IEmailOutbox?", emailSenderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("IEmailOutbox?", dietologistEmailSenderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("emailOutbox is null", emailSenderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("emailOutbox is null", dietologistEmailSenderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("IEmailTransport emailTransport", dietologistEmailSenderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotDependOnOptionsOrConfiguration() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] forbiddenPatterns = [
            "IOptions<",
            "IOptionsMonitor<",
            "IOptionsSnapshot<",
            "IConfiguration",
            "using Microsoft.Extensions.Options",
            "using Microsoft.Extensions.Configuration",
        ];

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

    private static string[] GetFilesIfDirectoryExists(string path, string searchPattern, SearchOption searchOption) =>
        Directory.Exists(path) ? Directory.GetFiles(path, searchPattern, searchOption) : [];

    private static string[] FindRepositoryReferenceViolations(
        string repositoryRoot,
        string applicationRoot,
        string typeName,
        IReadOnlyCollection<string> allowedDirectories) {
        return [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !allowedDirectories.Any(directory => path.StartsWith(directory, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains(typeName, StringComparison.Ordinal))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}"))];
    }

    private static string[] FindReferencesInFiles(string repositoryRoot, IReadOnlyCollection<string> files, string typeName) {
        return [.. files
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => entry.line.Contains(typeName, StringComparison.Ordinal))
                .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(repositoryRoot, entry.path)}:{entry.index + 1}")))];
    }

    private static IReadOnlyList<ConstructorParameter> ReadDomainEventHandlerConstructorParameters(string path) {
        string source = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        return [.. root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(IsDomainEventHandler)
            .SelectMany(classDeclaration => GetConstructorParameters(classDeclaration)
                .Select(parameter => new ConstructorParameter(
                    path,
                    tree.GetLineSpan(parameter.Span).StartLinePosition.Line + 1,
                    classDeclaration.Identifier.ValueText,
                    GetUnqualifiedTypeName(parameter.Type))))];
    }

    private static bool IsDomainEventHandler(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.BaseList?.Types.Any(static baseType =>
            baseType.Type.ToString().Contains(
                "INotificationHandler<NotificationEnvelope<",
                StringComparison.Ordinal)) == true;

    private static bool IsDomainEventHandlerSourceFile(string path) {
        string source = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Any(IsDomainEventHandler);
    }

    private static IEnumerable<ParameterSyntax> GetConstructorParameters(ClassDeclarationSyntax classDeclaration) {
        if (classDeclaration.ParameterList is not null) {
            foreach (ParameterSyntax parameter in classDeclaration.ParameterList.Parameters) {
                yield return parameter;
            }
        }

        foreach (ConstructorDeclarationSyntax constructor in classDeclaration.Members.OfType<ConstructorDeclarationSyntax>()) {
            foreach (ParameterSyntax parameter in constructor.ParameterList.Parameters) {
                yield return parameter;
            }
        }
    }

    private static string GetUnqualifiedTypeName(TypeSyntax? typeSyntax) {
        string typeName = typeSyntax?.ToString().TrimEnd('?') ?? string.Empty;
        int lastDot = typeName.LastIndexOf('.');

        return lastDot < 0 ? typeName : typeName[(lastDot + 1)..];
    }

    [ExcludeFromCodeCoverage]
    private sealed record ConstructorParameter(string Path, int Line, string ClassName, string TypeName);
}
