using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BusinessModuleBoundaryTests {
    [Fact]
    public void RootApplicationDependencyInjection_RemainsAModuleAggregator() {
        string dependencyInjectionPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "DependencyInjection.cs");
        string source = File.ReadAllText(dependencyInjectionPath);
        string[] expectedModuleCalls = [
            "services.AddAdministrationModules();",
            "services.AddIdentityModules();",
            "services.AddFoodModules();",
            "services.AddTrackingModules();",
            "services.AddNotificationModule();",
            "services.AddBillingModule();",
        ];

        foreach (string expectedModuleCall in expectedModuleCalls) {
            Assert.Contains(expectedModuleCall, source, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("using FoodDiary.Application.Admin", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using FoodDiary.Application.Billing", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using FoodDiary.Application.Notifications", source, StringComparison.Ordinal);
        Assert.Equal(1, source.Split("services.AddScoped<", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("services.TryAddScoped<", source, StringComparison.Ordinal);
    }

    private static readonly HashSet<string> ApprovedFastingApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Fasting",
        "FoodDiary.Application.Abstractions.Notifications.Common",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Fasting",
        "FoodDiary.Application.Notifications.Common",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedNotificationsApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Notifications",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Notifications",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedBillingApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Billing",
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Billing",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Marketing.Common",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedProductsApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Images.Common",
        "FoodDiary.Application.Abstractions.OpenFoodFacts.Models",
        "FoodDiary.Application.Abstractions.Products",
        "FoodDiary.Application.Abstractions.RecentItems.Common",
        "FoodDiary.Application.Abstractions.Usda",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.FavoriteProducts",
        "FoodDiary.Application.Images.Common",
        "FoodDiary.Application.OpenFoodFacts.Common",
        "FoodDiary.Application.Products",
        "FoodDiary.Application.RecentItems.Common",
        "FoodDiary.Application.Users.Common",
        "FoodDiary.Application.Usda.Common",
    };

    private static readonly HashSet<string> ApprovedRecipesApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Images.Common",
        "FoodDiary.Application.Abstractions.Products.Common",
        "FoodDiary.Application.Abstractions.RecentItems.Common",
        "FoodDiary.Application.Abstractions.Recipes",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.FavoriteRecipes",
        "FoodDiary.Application.Images.Common",
        "FoodDiary.Application.Nutrition.Common",
        "FoodDiary.Application.RecentItems.Common",
        "FoodDiary.Application.Recipes",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedConsumptionsApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.FavoriteMeals",
        "FoodDiary.Application.Abstractions.Images.Common",
        "FoodDiary.Application.Abstractions.Meals",
        "FoodDiary.Application.Abstractions.Products.Common",
        "FoodDiary.Application.Abstractions.RecentItems.Common",
        "FoodDiary.Application.Abstractions.Recipes.Common",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Consumptions",
        "FoodDiary.Application.FavoriteMeals",
        "FoodDiary.Application.Images.Common",
        "FoodDiary.Application.Nutrition.Common",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedUsersApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Authentication.Common",
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Dietologist",
        "FoodDiary.Application.Abstractions.Images.Common",
        "FoodDiary.Application.Abstractions.Users",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Dietologist",
        "FoodDiary.Application.Images.Common",
        "FoodDiary.Application.Notifications",
        "FoodDiary.Application.Users",
    };

    private static readonly HashSet<string> ApprovedAuthenticationApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Authentication",
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Email.Common",
        "FoodDiary.Application.Abstractions.Notifications.Common",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Authentication",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Email.Common",
        "FoodDiary.Application.Notifications.Common",
        "FoodDiary.Application.Users",
    };

    [Fact]
    public void FastingApplication_DoesNotDependOnUnapprovedApplicationFeatures() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            "Fasting");

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !ApprovedFastingApplicationDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FastingApplicationAbstractions_DoNotDependOnOtherFeatureContracts() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application.Abstractions",
            "Fasting");

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationAbstractionsNamespaceDependencies)
            .Where(dependency => !dependency.Namespace.Equals("FoodDiary.Application.Abstractions.Fasting", StringComparison.Ordinal) &&
                                 !dependency.Namespace.StartsWith("FoodDiary.Application.Abstractions.Fasting.", StringComparison.Ordinal) &&
                                 !dependency.Namespace.Equals("FoodDiary.Application.Abstractions.Common", StringComparison.Ordinal) &&
                                 !dependency.Namespace.StartsWith("FoodDiary.Application.Abstractions.Common.", StringComparison.Ordinal))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references foreign feature contract {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationsApplication_DoesNotDependOnUnapprovedApplicationFeatures() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            "Notifications");

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !ApprovedNotificationsApplicationDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationsApplicationAbstractions_DoNotDependOnOtherFeatureContracts() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application.Abstractions",
            "Notifications");

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationAbstractionsNamespaceDependencies)
            .Where(dependency => !dependency.Namespace.Equals("FoodDiary.Application.Abstractions.Notifications", StringComparison.Ordinal) &&
                                 !dependency.Namespace.StartsWith("FoodDiary.Application.Abstractions.Notifications.", StringComparison.Ordinal) &&
                                 !dependency.Namespace.Equals("FoodDiary.Application.Abstractions.Common", StringComparison.Ordinal) &&
                                 !dependency.Namespace.StartsWith("FoodDiary.Application.Abstractions.Common.", StringComparison.Ordinal))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references foreign feature contract {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireNotificationPersistenceRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string notificationsRoot = Path.Combine(applicationRoot, "Notifications");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenRepositoryContracts = [
            "INotificationRepository",
            "INotificationReadRepository",
            "INotificationReadModelRepository",
            "INotificationLookupRepository",
            "INotificationWriteRepository",
            "IWebPushSubscriptionRepository",
            "IWebPushSubscriptionReadRepository",
            "IWebPushSubscriptionReadModelRepository",
            "IWebPushSubscriptionWriteRepository",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(notificationsRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenRepositoryContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Notifications-owned persistence repository; use a semantic Notifications API")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FastingApplication_DoesNotAcquireNotificationReadModelRepository() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            "Fasting");

        string[] violations = SourceScanner.FindLinePatternViolations(
            moduleRoot,
            ["INotificationReadModelRepository"]);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Fasting")]
    [InlineData("Dietologist")]
    [InlineData("Users")]
    public void MigratedApplicationModules_DoNotAcquireNotificationReadModelRepositories(string moduleName) {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            moduleName);

        string[] violations = SourceScanner.FindLinePatternViolations(
            moduleRoot,
            [
                "INotificationReadModelRepository",
                "IWebPushSubscriptionReadModelRepository",
            ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireFastingRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string fastingRoot = Path.Combine(applicationRoot, "Fasting");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IFastingPlanRepository",
            "IFastingPlanReadRepository",
            "IFastingPlanWriteRepository",
            "IFastingOccurrenceRepository",
            "IFastingOccurrenceReadRepository",
            "IFastingOccurrenceReadModelRepository",
            "IFastingOccurrenceWriteRepository",
            "IFastingCheckInRepository",
            "IFastingCheckInReadRepository",
            "IFastingCheckInReadModelRepository",
            "IFastingCheckInWriteRepository",
            "IFastingSessionRepository",
            "IFastingSessionReadRepository",
            "IFastingSessionWriteRepository",
            "IFastingTelemetryEventRepository",
            "IFastingTelemetryEventReadRepository",
            "IFastingTelemetryEventWriteRepository",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(fastingRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Fasting-owned repository; use a semantic Fasting API")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FastingInfrastructureImplementations_StayInOwnedFolders() {
        string infrastructureRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Infrastructure");
        string[] fastingInfrastructureFiles = [.. SourceScanner.SourceFiles(infrastructureRoot)
            .Where(path => Path.GetFileName(path).StartsWith("Fasting", StringComparison.Ordinal))];
        string[] approvedDirectories = [
            Path.Combine(infrastructureRoot, "Persistence", "Tracking"),
            Path.Combine(infrastructureRoot, "Persistence", "Configurations", "Tracking"),
        ];

        string[] violations = [.. fastingInfrastructureFiles
            .Where(path => !approvedDirectories.Any(directory =>
                Path.GetDirectoryName(path)?.Equals(directory, StringComparison.OrdinalIgnoreCase) == true))
            .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingApplication_DoesNotDependOnUnapprovedApplicationFeatures() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            "Billing");

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !ApprovedBillingApplicationDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireBillingRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string billingRoot = Path.Combine(applicationRoot, "Billing");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IBillingSubscriptionRepository",
            "IBillingSubscriptionReadRepository",
            "IBillingSubscriptionReadModelRepository",
            "IBillingSubscriptionWriteRepository",
            "IBillingPaymentRepository",
            "IBillingPaymentReadRepository",
            "IBillingPaymentWriteRepository",
            "IBillingWebhookEventRepository",
            "IBillingWebhookEventReadRepository",
            "IBillingWebhookEventWriteRepository",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(billingRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Billing-owned repository; use a semantic Billing API")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void JobManager_UsesBillingModuleApiInsteadOfConcreteApplicationService() {
        string jobManagerRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.JobManager");

        string[] violations = SourceScanner.FindLinePatternViolations(
            jobManagerRoot,
            [
                "FoodDiary.Application.Billing.Services",
                "    BillingRenewalService ",
            ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingInfrastructureImplementations_StayInOwnedFolders() {
        string infrastructureRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Infrastructure");
        string persistenceRoot = Path.Combine(infrastructureRoot, "Persistence");
        string[] billingInfrastructureFiles = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(path => Path.GetFileName(path).StartsWith("Billing", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Admin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];
        string[] approvedDirectories = [
            Path.Combine(persistenceRoot, "Billing"),
            Path.Combine(persistenceRoot, "Configurations", "Billing"),
        ];
        string approvedDbContextPartial = Path.Combine(persistenceRoot, "FoodDiaryDbContext.Billing.cs");

        string[] violations = [.. billingInfrastructureFiles
            .Where(path => !path.Equals(approvedDbContextPartial, StringComparison.OrdinalIgnoreCase))
            .Where(path => !approvedDirectories.Any(directory =>
                Path.GetDirectoryName(path)?.Equals(directory, StringComparison.OrdinalIgnoreCase) == true))
            .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(CatalogModuleDependencies))]
    public void CatalogModules_DoNotDependOnUnapprovedApplicationFeatures(
        string moduleName,
        IReadOnlySet<string> approvedDependencies) {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            moduleName);

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !approvedDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    public static TheoryData<string, IReadOnlySet<string>> CatalogModuleDependencies => new() {
        { "Products", ApprovedProductsApplicationDependencies },
        { "Recipes", ApprovedRecipesApplicationDependencies },
    };

    [Theory]
    [InlineData("Products", "IProductRepository", "IProductReadRepository", "IProductWriteRepository")]
    [InlineData("Recipes", "IRecipeRepository", "IRecipeReadRepository", "IRecipeWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireCatalogAggregateRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string ownerRoot = Path.Combine(applicationRoot, ownerModule);
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(ownerRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a {ownerModule}-owned aggregate repository; use its lookup/access/read service API")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FoodPersistenceComposition_DelegatesToOwnedRegistrationModules() {
        string path = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "DependencyInjection.Food.cs");
        string source = File.ReadAllText(path);

        Assert.Contains(".AddProductsPersistence()", source, StringComparison.Ordinal);
        Assert.Contains(".AddRecipesPersistence()", source, StringComparison.Ordinal);
        Assert.Contains(".AddRecentItemsPersistence()", source, StringComparison.Ordinal);
        Assert.Contains(".AddMealsPersistence()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ProductConfiguration.cs", "Configurations/Products")]
    [InlineData("RecipeConfiguration.cs", "Configurations/Recipes")]
    [InlineData("RecipeIngredientConfiguration.cs", "Configurations/Recipes")]
    [InlineData("RecipeStepConfiguration.cs", "Configurations/Recipes")]
    public void CatalogAggregateConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Fact]
    public void ConsumptionsApplication_DoesNotDependOnUnapprovedApplicationFeatures() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            "Consumptions");

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !ApprovedConsumptionsApplicationDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireMealPersistenceRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string ownerRoot = Path.Combine(applicationRoot, "Consumptions");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IMealRepository",
            "IMealReadRepository",
            "IMealWriteRepository",
            "IMealActivityReadRepository",
            "IMealConsumptionReadRepository",
            "IMealProductNutritionReadRepository",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(ownerRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Consumption-owned Meal repository; use a semantic Consumption read capability")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireRecentItemRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IRecentItemRepository",
            "IRecentItemReadRepository",
            "IRecentItemWriteRepository",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a RecentItems repository; use IRecentItemUsageReadService or IRecentItemUsageRecorder")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("MealConfiguration.cs", "Configurations/Meals")]
    [InlineData("MealItemConfiguration.cs", "Configurations/Meals")]
    [InlineData("MealAiSessionConfiguration.cs", "Configurations/Meals")]
    [InlineData("MealAiItemConfiguration.cs", "Configurations/Meals")]
    [InlineData("RecentItemConfiguration.cs", "Configurations/RecentItems")]
    public void ConsumptionAndRecentItemConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [MemberData(nameof(IdentityModuleDependencies))]
    public void IdentityModules_DoNotDependOnUnapprovedApplicationFeatures(
        string moduleName,
        IReadOnlySet<string> approvedDependencies) {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Application",
            moduleName);

        string[] violations = [.. SourceScanner.SourceFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !approvedDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    public static TheoryData<string, IReadOnlySet<string>> IdentityModuleDependencies => new() {
        { "Users", ApprovedUsersApplicationDependencies },
        { "Authentication", ApprovedAuthenticationApplicationDependencies },
    };

    [Fact]
    public void OtherApplicationModules_DoNotAcquireCoreUserRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string usersRoot = Path.Combine(applicationRoot, "Users");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IUserRepository",
            "IUserLookupRepository",
            "IUserWriteRepository",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(usersRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Users-owned repository; use IUserDirectoryService or a Users-owned mutation capability")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("UserConfiguration.cs", "Configurations/Users")]
    [InlineData("RoleConfiguration.cs", "Configurations/Users")]
    [InlineData("UserRoleConfiguration.cs", "Configurations/Users")]
    [InlineData("UserRoleAuditEventConfiguration.cs", "Configurations/Users")]
    [InlineData("UserRefreshTokenSessionConfiguration.cs", "Configurations/Authentication")]
    [InlineData("UserLoginEventConfiguration.cs", "Configurations/Authentication")]
    public void IdentityConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireImageRepositories() {
        AssertNoForeignRepositoryDependencies(
            "Images",
            ["IImageAssetRepository", "IImageAssetReadRepository", "IImageAssetWriteRepository"],
            "use IImageAssetAccessService or IImageAssetCleanupService");
    }

    [Theory]
    [InlineData("FavoriteProducts", "IFavoriteProductRepository", "IFavoriteProductReadRepository", "IFavoriteProductReadModelRepository", "IFavoriteProductWriteRepository")]
    [InlineData("FavoriteRecipes", "IFavoriteRecipeRepository", "IFavoriteRecipeReadRepository", "IFavoriteRecipeReadModelRepository", "IFavoriteRecipeWriteRepository")]
    [InlineData("FavoriteMeals", "IFavoriteMealRepository", "IFavoriteMealReadRepository", "IFavoriteMealReadModelRepository", "IFavoriteMealWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireFavoriteRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning Favorite read service or command");
    }

    [Theory]
    [InlineData("ImageAssetConfiguration.cs", "Configurations/Images")]
    [InlineData("ImageObjectDeletionOutboxMessageConfiguration.cs", "Configurations/Images")]
    [InlineData("FavoriteProductConfiguration.cs", "Configurations/Favorites")]
    [InlineData("FavoriteRecipeConfiguration.cs", "Configurations/Favorites")]
    [InlineData("FavoriteMealConfiguration.cs", "Configurations/Favorites")]
    public void ImageAndFavoriteConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireDietologistRepositories() {
        AssertNoForeignRepositoryDependencies(
            "Dietologist",
            [
                "IDietologistInvitationRepository",
                "IDietologistInvitationReadRepository",
                "IDietologistInvitationReadModelRepository",
                "IDietologistInvitationWriteRepository",
                "IRecommendationRepository",
                "IRecommendationReadRepository",
                "IRecommendationReadModelRepository",
                "IRecommendationWriteRepository",
            ],
            "use a Dietologist relationship/recommendation service");
    }

    [Theory]
    [InlineData("RecipeComments", "IRecipeCommentRepository", "IRecipeCommentReadRepository", "IRecipeCommentReadModelRepository", "IRecipeCommentWriteRepository")]
    [InlineData("RecipeLikes", "IRecipeLikeRepository", "IRecipeLikeReadRepository", "IRecipeLikeWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireRecipeSocialRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning social interaction service or command");
    }

    [Theory]
    [InlineData("DietologistInvitationConfiguration.cs", "Configurations/Dietologist")]
    [InlineData("RecommendationConfiguration.cs", "Configurations/Dietologist")]
    [InlineData("RecipeCommentConfiguration.cs", "Configurations/RecipeSocial")]
    [InlineData("RecipeLikeConfiguration.cs", "Configurations/RecipeSocial")]
    public void DietologistAndRecipeSocialConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [InlineData("WeightEntries", "IWeightEntryRepository", "IWeightEntryReadRepository", "IWeightEntryReadModelRepository", "IWeightEntryWriteRepository")]
    [InlineData("WaistEntries", "IWaistEntryRepository", "IWaistEntryReadRepository", "IWaistEntryReadModelRepository", "IWaistEntryWriteRepository")]
    [InlineData("Hydration", "IHydrationEntryRepository", "IHydrationEntryReadRepository", "IHydrationEntryReadModelRepository", "IHydrationEntryWriteRepository")]
    [InlineData("Exercises", "IExerciseEntryRepository", "IExerciseEntryReadRepository", "IExerciseEntryReadModelRepository", "IExerciseEntryWriteRepository")]
    [InlineData("Cycles", "ICycleRepository", "ICycleReadRepository", "ICycleReadModelRepository", "ICycleWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireHealthTrackingWriteRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning health module command or read/projection service");
    }

    [Theory]
    [InlineData("WeightEntryConfiguration.cs", "Configurations/BodyMetrics")]
    [InlineData("WaistEntryConfiguration.cs", "Configurations/BodyMetrics")]
    [InlineData("HydrationEntryConfiguration.cs", "Configurations/Hydration")]
    [InlineData("ExerciseEntryConfiguration.cs", "Configurations/Exercises")]
    [InlineData("CycleProfileConfiguration.cs", "Configurations/Cycles")]
    [InlineData("CycleFactorConfiguration.cs", "Configurations/Cycles")]
    [InlineData("CycleSymptomEntryConfiguration.cs", "Configurations/Cycles")]
    [InlineData("BleedingEntryConfiguration.cs", "Configurations/Cycles")]
    [InlineData("FertilitySignalConfiguration.cs", "Configurations/Cycles")]
    public void HealthTrackingConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [InlineData("ShoppingLists", "IShoppingListRepository", "IShoppingListReadRepository", "IShoppingListReadModelRepository", "IShoppingListWriteRepository")]
    [InlineData("MealPlans", "IMealPlanRepository", "IMealPlanReadRepository", "IMealPlanReadModelRepository", "IMealPlanWriteRepository")]
    [InlineData("Marketing", "IMarketingAttributionEventRepository", "IMarketingAttributionEventReadRepository", "IMarketingAttributionEventWriteRepository")]
    public void OtherApplicationModules_DoNotAcquirePlanningOrMarketingRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning module command, read service or capability");
    }

    [Theory]
    [InlineData("Wearables", "IWearableConnectionRepository", "IWearableConnectionReadRepository", "IWearableConnectionWriteRepository")]
    [InlineData("Wearables", "IWearableSyncRepository", "IWearableSyncReadRepository", "IWearableSyncReadModelRepository", "IWearableSyncWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireWearableRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the Wearables command or read-service boundary");
    }

    [Theory]
    [InlineData("ShoppingListConfiguration.cs", "Configurations/ShoppingLists")]
    [InlineData("ShoppingListItemConfiguration.cs", "Configurations/ShoppingLists")]
    [InlineData("ShoppingListItemSourceConfiguration.cs", "Configurations/ShoppingLists")]
    [InlineData("MealPlanConfiguration.cs", "Configurations/MealPlans")]
    [InlineData("MealPlanDayConfiguration.cs", "Configurations/MealPlans")]
    [InlineData("MealPlanMealConfiguration.cs", "Configurations/MealPlans")]
    [InlineData("WearableConnectionConfiguration.cs", "Configurations/Wearables")]
    [InlineData("WearableSyncEntryConfiguration.cs", "Configurations/Wearables")]
    [InlineData("MarketingAttributionEventConfiguration.cs", "Configurations/Marketing")]
    public void PlanningWearablesAndMarketingConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [InlineData("Lessons", "INutritionLessonRepository", "INutritionLessonReadRepository", "INutritionLessonWriteRepository")]
    [InlineData("DailyAdvices", "IDailyAdviceRepository", "IDailyAdviceReadRepository")]
    public void OtherApplicationModules_DoNotAcquireContentAggregateRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning content module administration or read capability");
    }

    [Theory]
    [InlineData("NutritionLessonConfiguration.cs", "Configurations/Lessons")]
    [InlineData("UserLessonProgressConfiguration.cs", "Configurations/Lessons")]
    [InlineData("DailyAdviceConfiguration.cs", "Configurations/DailyAdvices")]
    public void ContentConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Fact]
    public void EntityConfigurations_AreGroupedByOwningModule() {
        string configurationsRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "FoodDiary.Infrastructure",
            "Persistence",
            "Configurations");

        string[] unownedConfigurations = [.. Directory
            .EnumerateFiles(configurationsRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unownedConfigurations);
    }

    [Theory]
    [InlineData("ContentReports", "IContentReportRepository", "IContentReportWriteRepository")]
    [InlineData("Ai", "IAiPromptTemplateRepository", "IAiPromptTemplateReadRepository", "IAiPromptTemplateWriteRepository")]
    [InlineData("Email", "IEmailTemplateRepository", "IEmailTemplateWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireAdministrativeContentWriteRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning module administration capability; projection-only admin reads remain allowed");
    }

    [Theory]
    [InlineData("OpenFoodFacts", "IOpenFoodFactsProductCacheRepository", "IOpenFoodFactsProductCacheReadRepository", "IOpenFoodFactsProductCacheWriteRepository")]
    [InlineData("Usda", "IUsdaFoodRepository", "IUsdaFoodReadRepository", "IUsdaFoodReadModelRepository", "IUsdaProductLinkRepository", "IUsdaProductLinkReadRepository", "IUsdaProductLinkWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireExternalCatalogRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the external catalog read service or search-suggestion projection");
    }

    [Theory]
    [InlineData("Ai", "IAiUsageReadRepository", "Admin/Services/AdminAiUsageReadService.cs")]
    [InlineData("Lessons", "INutritionLessonReadModelRepository", "Admin/Services/AdminContentReadService.cs")]
    [InlineData("Email", "IEmailTemplateReadModelRepository", "Admin/Services/AdminContentReadService.cs")]
    [InlineData("Ai", "IAiPromptTemplateReadModelRepository", "Admin/Services/AdminContentReadService.cs")]
    [InlineData("ContentReports", "IContentReportReadModelRepository", "Admin/Services/AdminContentReadService.cs")]
    [InlineData("ContentReports", "IContentReportReadRepository", "Admin/Services/AdminDashboardReadService.cs")]
    [InlineData("Authentication", "IUserLoginEventReadRepository", "Admin/Services/AdminUserLoginReadService.cs")]
    [InlineData("Users", "IUserAdminReadModelRepository", "Admin/Services/AdminUserReadService.cs")]
    public void CrossModuleRepositoryProjections_AreExplicitlyAllowlisted(
        string ownerModule,
        string projectionContract,
        params string[] allowedConsumerPaths) {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string ownerRoot = Path.Combine(applicationRoot, ownerModule);
        string dependencyInjectionPath = Path.Combine(applicationRoot, "DependencyInjection.cs");

        string[] actualConsumers = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(ownerRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Equals(dependencyInjectionPath, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadLines(path).Any(line => line.Contains(projectionContract, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(applicationRoot, path).Replace('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
        string[] expectedConsumers = [.. allowedConsumerPaths.Order(StringComparer.OrdinalIgnoreCase)];

        Assert.Equal(expectedConsumers, actualConsumers, StringComparer.OrdinalIgnoreCase);
    }

    private static void AssertNoForeignRepositoryDependencies(
        string ownerModule,
        IReadOnlyCollection<string> forbiddenContracts,
        string guidance) {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string ownerRoot = Path.Combine(applicationRoot, ownerModule);
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(ownerRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a {ownerModule}-owned repository; {guidance}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static IEnumerable<NamespaceDependency> ReadApplicationNamespaceDependencies(string path) =>
        ReadNamespaceDependencies(path, "FoodDiary.Application.");

    private static IEnumerable<NamespaceDependency> ReadApplicationAbstractionsNamespaceDependencies(string path) =>
        ReadNamespaceDependencies(path, "FoodDiary.Application.Abstractions.");

    private static IEnumerable<NamespaceDependency> ReadNamespaceDependencies(string path, string prefix) {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        return root.Usings
            .Select(usingDirective => new {
                Namespace = usingDirective.Name?.ToString() ?? string.Empty,
                Line = tree.GetLineSpan(usingDirective.Span).StartLinePosition.Line + 1,
            })
            .Where(entry => entry.Namespace.StartsWith(prefix, StringComparison.Ordinal))
            .Select(entry => new NamespaceDependency(path, entry.Line, entry.Namespace));
    }

    [ExcludeFromCodeCoverage]
    private sealed record NamespaceDependency(string Path, int Line, string Namespace);
}
