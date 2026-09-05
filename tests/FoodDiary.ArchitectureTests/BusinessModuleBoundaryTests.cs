using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BusinessModuleBoundaryTests {
    [Fact]
    public void ExtractedModuleOwnedTests_DoNotReturnToHorizontalDonorProjects() {
        string[] forbiddenApplicationDirectories = ["Fasting", "Hydration", "Tdee", "WeeklyGoals", "Notifications"];
        string applicationTestsRoot = ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Application.Tests");
        string domainTestsRoot = ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Domain.Tests");
        string[] infrastructureTestRoots = [
            ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Infrastructure.Tests"),
            ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Infrastructure.IntegrationTests"),
        ];

        string[] violations = [
            .. forbiddenApplicationDirectories
                .Select(name => Path.Combine(applicationTestsRoot, name))
                .Where(Directory.Exists)
                .Select(path => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)} is a module-owned application test directory"),
            .. SourceScanner.SourceFiles(domainTestsRoot)
                .Where(path => Path.GetFileName(path).StartsWith("Fasting", StringComparison.Ordinal))
                .Select(path => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)} is a Fasting-owned domain test"),
            .. infrastructureTestRoots
                .SelectMany(SourceScanner.SourceFiles)
                .Where(path => Path.GetFileName(path).StartsWith("Fasting", StringComparison.Ordinal) ||
                    Path.GetFileName(path).StartsWith("Hydration", StringComparison.Ordinal) ||
                    Path.GetFileName(path).StartsWith("WeeklyGoal", StringComparison.Ordinal))
                .Select(path => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)} is an extracted-module infrastructure test"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationRuntimeDependencyInjection_StaysFreeOfFeatureModuleRegistration() {
        string dependencyInjectionPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Runtime",
            "DependencyInjection.cs");
        string source = File.ReadAllText(dependencyInjectionPath);

        Assert.DoesNotContain("using FoodDiary.Application.Admin", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using FoodDiary.Application.Billing", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using FoodDiary.Application.Notifications", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddValidatorsFromAssembly", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterServicesFromAssembly", source, StringComparison.Ordinal);
        Assert.Equal(1, source.Split("services.AddScoped<", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("services.TryAddScoped<", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UsersProfileReadContracts_LiveInUsersContracts() {
        string legacyModelsRoot = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Users",
            "Models");
        string legacyContractPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Users",
            "Common",
            "IUserProfileReadService.cs");
        string abstractionContractPath = ArchitectureTestPaths.FromRoot(
            "Modules",
            "Users",
            "Contracts",
            "Users",
            "Common",
            "IUserProfileReadService.cs");

        Assert.Empty(Directory.Exists(legacyModelsRoot) ? SourceScanner.SourceFiles(legacyModelsRoot) : []);
        Assert.False(File.Exists(legacyContractPath));
        Assert.True(File.Exists(abstractionContractPath));

        string[] productionRoots = [
            .. ModuleSourceCatalog.ApplicationRoots.Values,
            ArchitectureTestPaths.FromRoot("FoodDiary.Presentation.Api"),
        ];
        string[] violations = [.. productionRoots
            .SelectMany(SourceScanner.SourceFiles)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .Where(static entry => entry.line.Contains("FoodDiary.Application.Users.Models", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} references the legacy Users model namespace")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedUserProfileConsumers_DoNotDependOnInternalUserContextService() {
        string[] migratedModules = ["Ai", "Dashboard", "Dietologist", "Gamification", "Hydration", "Tdee", "WeeklyCheckIn"];

        string[] violations = [.. migratedModules
            .Select(ModuleSourceCatalog.ApplicationRoot)
            .SelectMany(SourceScanner.SourceFiles)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .Where(static entry => entry.line.Contains("IUserContextService", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} references internal Users aggregate access")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAuthenticationIdentityHandlers_DoNotDependOnUsersAggregateAccess() {
        string[] handlerPaths = [
            "Modules/Identity/Application/Authentication/Commands/ConfirmPasswordReset/ConfirmPasswordResetCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/AdminSsoExchange/AdminSsoExchangeCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/AdminSsoStart/AdminSsoStartCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/GoogleLogin/GoogleLoginCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/LinkGoogle/LinkGoogleCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/LinkTelegram/LinkTelegramCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/Login/LoginCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/RequestPasswordReset/RequestPasswordResetCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/Register/RegisterCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/ResendEmailVerification/ResendEmailVerificationCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/RestoreAccount/RestoreAccountCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/TelegramBotAuth/TelegramBotAuthCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/TelegramLoginWidget/TelegramLoginWidgetCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/TelegramVerify/TelegramVerifyCommandHandler.cs",
            "Modules/Identity/Application/Authentication/Commands/VerifyEmail/VerifyEmailCommandHandler.cs",
        ];
        string[] forbiddenReferences = [
            "FoodDiary.Domain.Entities.Users",
            "IAuthenticationUserMutationService",
            "IUserContextService",
        ];

        string[] violations = [.. handlerPaths
            .Select(path => Path.Combine(ArchitectureTestPaths.RepositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .SelectMany(entry => forbiddenReferences
                .Where(entry.line.Contains)
                .Select(reference => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} references {reference}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedRefreshTokenHandler_DoesNotLoadOrMutateUserAggregate() {
        string handlerPath = ArchitectureTestPaths.FromRoot(
            "Modules",
            "Identity",
            "Application",
            "Authentication",
            "Commands",
            "RefreshToken",
            "RefreshTokenCommandHandler.cs");
        string source = File.ReadAllText(handlerPath);
        string[] forbiddenReferences = [
            "IAuthenticationUserLookupService",
            "IAuthenticationUserMutationService",
            "IssueAndStoreAsync",
            "User? user",
            "User currentUser",
        ];

        string[] violations = [.. forbiddenReferences
            .Where(source.Contains)
            .Select(reference => $"RefreshTokenCommandHandler references {reference}")];

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminUserMutationHandlers_DoNotDependOnUsersAggregateAccess() {
        string[] handlerPaths = [
            "Modules/Admin/Application/Commands/CreateAdminUser/CreateAdminUserCommandHandler.cs",
            "Modules/Admin/Application/Commands/SetAdminUserPassword/SetAdminUserPasswordCommandHandler.cs",
            "Modules/Admin/Application/Commands/StartAdminImpersonation/StartAdminImpersonationCommandHandler.cs",
            "Modules/Admin/Application/Commands/UpdateAdminUser/UpdateAdminUserCommandHandler.cs",
        ];
        string[] forbiddenReferences = [
            "FoodDiary.Domain.Entities.Users",
            "IAdminImpersonationUserService",
            "IAdminUserManagementService",
            "IUserAdministrationService",
        ];

        string[] violations = FindReferences(handlerPaths, forbiddenReferences);

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationsModule_DoesNotDependOnUsersAggregateAccess() {
        string notificationsRoot = ArchitectureTestPaths.FromRoot("Modules", "Notifications", "Application");
        string[] forbiddenReferences = [
            "FoodDiary.Domain.Entities.Users",
            "INotificationUserAccessService",
            "IUserLookupRepository",
            "IUserWriteRepository",
        ];
        string[] violations = FindReferences(
            SourceScanner.SourceFiles(notificationsRoot)
                .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)),
            forbiddenReferences);

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingModule_DoesNotDependOnUsersAggregateAccess() {
        string billingRoot = ModuleSourceCatalog.ApplicationRoot("Billing");
        string[] forbiddenReferences = [
            "FoodDiary.Domain.Entities.Users",
            "IBillingUserLookupService",
            "IUserLookupRepository",
            "IUserWriteRepository",
        ];
        string[] violations = FindReferences(
            SourceScanner.SourceFiles(billingRoot)
                .Select(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)),
            forbiddenReferences);

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotReferenceInternalUsersNamespace() {
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");
        string usersRoot = ModuleSourceCatalog.ApplicationRoot("Users");
        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
            .Where(path => !path.StartsWith(usersRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .Where(static entry => entry.line.Contains("FoodDiary.Application.Users.", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} references internal Users implementation namespace")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static string[] FindReferences(IEnumerable<string> relativePaths, IReadOnlyCollection<string> references) =>
        [.. relativePaths
            .Select(path => Path.Combine(ArchitectureTestPaths.RepositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .SelectMany(entry => references
                .Where(entry.line.Contains)
                .Select(reference => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} references {reference}"))
            .Order(StringComparer.Ordinal)];

    private static readonly HashSet<string> ApprovedFastingApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Fasting",
        "FoodDiary.Application.Abstractions.Notifications.Common",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Modules.Fasting.Application",
        "FoodDiary.Application.Notifications.Common",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedNotificationsApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Dietologist.Common.DietologistErrors",
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Notifications",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Notifications",
        "FoodDiary.Application.Abstractions.Users.Models",
    };

    private static readonly HashSet<string> ApprovedBillingApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Billing",
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Users",
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
        "FoodDiary.Application.Abstractions.FavoriteProducts",
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
        "FoodDiary.Application.Abstractions.Products.Models",
        "FoodDiary.Application.Abstractions.RecentItems.Common",
        "FoodDiary.Application.Abstractions.Recipes",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Abstractions.FavoriteRecipes",
        "FoodDiary.Application.Images.Common",
        "FoodDiary.Application.Abstractions.Nutrition.Common",
        "FoodDiary.Application.RecentItems.Common",
        "FoodDiary.Application.Recipes",
        "FoodDiary.Application.Users.Common",
    };

    private static readonly HashSet<string> ApprovedMealsApplicationDependencies = new(StringComparer.Ordinal) {
        "FoodDiary.Application.Abstractions.Achievements.Common",
        "FoodDiary.Application.Abstractions.Common",
        "FoodDiary.Application.Abstractions.Meals",
        "FoodDiary.Application.Meals.Common",
        "FoodDiary.Application.Abstractions.FavoriteMeals",
        "FoodDiary.Application.Abstractions.Images.Common",
        "FoodDiary.Application.Abstractions.Meals",
        "FoodDiary.Application.Abstractions.Products.Common",
        "FoodDiary.Application.Abstractions.Products.Models",
        "FoodDiary.Application.Abstractions.RecentItems.Common",
        "FoodDiary.Application.Abstractions.Recipes.Common",
        "FoodDiary.Application.Abstractions.Recipes.Models",
        "FoodDiary.Application.Abstractions.Users.Common",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Meals",
        "FoodDiary.Application.Images.Common",
        "FoodDiary.Modules.Meals.Application.Abstractions",
        "FoodDiary.Modules.Meals.Contracts",
        "FoodDiary.Application.Abstractions.Nutrition.Common",
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
        "FoodDiary.Application.Abstractions.Users.Models",
        "FoodDiary.Application.Authentication",
        "FoodDiary.Application.Identity.Authentication",
        "FoodDiary.Application.Common",
        "FoodDiary.Application.Abstractions.Admin.Common",
        "FoodDiary.Application.Notifications.Common",
        "FoodDiary.Application.Users",
    };

    [Fact]
    public void FastingApplication_DoesNotDependOnUnapprovedApplicationFeatures() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "Modules",
            "Fasting",
            "Application");

        string[] violations = [.. ModuleSourceCatalog.RequiredFiles(moduleRoot)
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
        string moduleRoot = Path.Combine(ModuleSourceCatalog.ApplicationRoot("Fasting"), "Abstractions");

        string[] violations = [.. ModuleSourceCatalog.RequiredFiles(moduleRoot)
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
            "Modules", "Notifications", "Application");

        string[] violations = [.. ModuleSourceCatalog.RequiredFiles(moduleRoot)
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
            "Modules", "Notifications", "Application", "Abstractions");

        string[] violations = [.. ModuleSourceCatalog.RequiredFiles(moduleRoot)
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
        string notificationsRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "Modules", "Notifications", "Application");
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

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
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
            "Modules",
            "Fasting",
            "Application");

        string[] violations = SourceScanner.FindLinePatternViolations(
            moduleRoot,
            ["INotificationReadModelRepository"]);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Dietologist")]
    [InlineData("Users")]
    public void MigratedApplicationModules_DoNotAcquireNotificationReadModelRepositories(string moduleName) {
        string moduleRoot = ModuleSourceCatalog.ApplicationRoot(moduleName);

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
        string fastingRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "Modules", "Fasting", "Application");
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

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
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
        string moduleRoot = ModuleSourceCatalog.ApplicationRoot("Billing");

        string[] violations = [.. ModuleSourceCatalog.RequiredFiles(moduleRoot)
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
        string billingRoot = ModuleSourceCatalog.ApplicationRoot("Billing");
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

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
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
        string moduleRoot = moduleName.Equals("Authentication", StringComparison.Ordinal)
            ? Path.Combine(ModuleSourceCatalog.ApplicationRoot(moduleName), "Authentication")
            : ModuleSourceCatalog.ApplicationRoot(moduleName);

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles(moduleRoot)
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
        string ownerRoot = ModuleSourceCatalog.ApplicationRoot(ownerModule);
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
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
    public void FoodPersistenceComposition_RemainsOwnedByModules() {
        string path = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "DependencyInjection.cs");
        string source = File.ReadAllText(path);

        Assert.DoesNotContain(".AddProductsPersistence()", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".AddRecipesPersistence()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddRecentItems", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".AddMealsPersistence()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddFoodPersistence", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ProductConfiguration.cs", "Modules/Products/Infrastructure/Model/Configurations/Products")]
    [InlineData("RecipeConfiguration.cs", "Modules/Recipes/Infrastructure/Model/Configurations/Recipes")]
    [InlineData("RecipeIngredientConfiguration.cs", "Modules/Recipes/Infrastructure/Model/Configurations/Recipes")]
    [InlineData("RecipeStepConfiguration.cs", "Modules/Recipes/Infrastructure/Model/Configurations/Recipes")]
    public void CatalogAggregateConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedRoot = expectedRelativeDirectory.StartsWith("Modules/", StringComparison.Ordinal)
            ? ArchitectureTestPaths.RepositoryRoot
            : Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Infrastructure", "Persistence");
        string expectedPath = Path.Combine(expectedRoot, expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar), fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Fact]
    public void MealsApplication_DoesNotDependOnUnapprovedApplicationFeatures() {
        string moduleRoot = Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "Modules",
            "Meals",
            "Application");

        string[] violations = [.. ModuleSourceCatalog.RequiredFiles(moduleRoot)
            .SelectMany(ReadApplicationNamespaceDependencies)
            .Where(dependency => !ApprovedMealsApplicationDependencies.Any(approved =>
                dependency.Namespace.Equals(approved, StringComparison.Ordinal) ||
                dependency.Namespace.StartsWith($"{approved}.", StringComparison.Ordinal)))
            .Select(dependency => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, dependency.Path)}:{dependency.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} references unapproved module namespace {dependency.Namespace}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotAcquireMealPersistenceRepositories() {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string ownerRoot = ModuleSourceCatalog.ApplicationRoot("Meals");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IMealRepository",
            "IMealReadRepository",
            "IMealWriteRepository",
            "IMealActivityReadRepository",
            "IMealProjectionReadRepository",
            "IMealProductNutritionReadRepository",
        ];

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
            .Where(path => !path.StartsWith(ownerRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Meal-owned Meal repository; use a semantic Meal read capability")
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

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
            .Where(path => !path.StartsWith(ModuleSourceCatalog.ApplicationRoot("RecentItems"), StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a RecentItems repository; use IRecentItemUsageReadService or IRecentItemUsageRecorder")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("MealConfiguration.cs", "Modules/Meals/Infrastructure/Model/Configurations/Meals")]
    [InlineData("MealItemConfiguration.cs", "Modules/Meals/Infrastructure/Model/Configurations/Meals")]
    [InlineData("MealAiSessionConfiguration.cs", "Modules/Meals/Infrastructure/Model/Configurations/Meals")]
    [InlineData("MealAiItemConfiguration.cs", "Modules/Meals/Infrastructure/Model/Configurations/Meals")]
    [InlineData("RecentItemConfiguration.cs", "Modules/RecentItems/Infrastructure/Model/Configurations/RecentItems")]
    public void MealAndRecentItemConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedRoot = expectedRelativeDirectory.StartsWith("Modules/", StringComparison.Ordinal)
            ? ArchitectureTestPaths.RepositoryRoot
            : Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Infrastructure", "Persistence");
        string expectedPath = Path.Combine(expectedRoot, expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar), fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [MemberData(nameof(IdentityModuleDependencies))]
    public void IdentityModules_DoNotDependOnUnapprovedApplicationFeatures(
        string moduleName,
        IReadOnlySet<string> approvedDependencies) {
        string moduleRoot = moduleName.Equals("Authentication", StringComparison.Ordinal)
            ? Path.Combine(ModuleSourceCatalog.ApplicationRoot(moduleName), "Authentication")
            : ModuleSourceCatalog.ApplicationRoot(moduleName);

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles(moduleRoot)
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
        string usersRoot = ModuleSourceCatalog.ApplicationRoot("Users");
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");
        string[] forbiddenContracts = [
            "IUserRepository",
            "IUserLookupRepository",
            "IUserWriteRepository",
        ];

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
            .Where(path => !path.StartsWith(usersRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).StartsWith("DependencyInjection", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenContracts.Any(contract => entry.line.Contains(contract, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} acquires a Users-owned repository; use a narrow Users-owned capability")
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
        string expectedPath = string.Equals(expectedRelativeDirectory, "Configurations/Users", StringComparison.Ordinal)
            ? ArchitectureTestPaths.FromRoot("Modules", "Users", "Infrastructure", "Model", "Persistence", "Configurations", "Users", fileName)
            : ArchitectureTestPaths.FromRoot("Modules", "Identity", "Infrastructure", "Model", "Configurations", "Authentication", fileName);

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
        string expectedPath;
        if (fileName.StartsWith("Favorite", StringComparison.Ordinal)) {
            expectedPath = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "Modules", "Favorites", "Infrastructure", "Model", "Configurations", fileName);
        } else if (string.Equals(fileName, "ImageAssetConfiguration.cs", StringComparison.Ordinal)) {
            expectedPath = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "Modules", "Images", "Infrastructure", "Model", "Configurations", fileName);
        } else {
            expectedPath = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "Modules", "Images", "Infrastructure", "Model", expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar), fileName);
        }

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
        string expectedPath = string.Equals(expectedRelativeDirectory, "Configurations/Dietologist", StringComparison.Ordinal)
            ? Path.Combine(ArchitectureTestPaths.RepositoryRoot, "Modules", "Dietologist", "Infrastructure", "Model", "Configurations", "Dietologist", fileName)
            : Path.Combine(
                ArchitectureTestPaths.RepositoryRoot,
                "Modules", "RecipeCommunity", "Infrastructure", "Model",
                expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
                fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [InlineData("WeightEntries", "IWeightEntryRepository", "IWeightEntryReadRepository", "IWeightEntryReadModelRepository", "IWeightEntryWriteRepository")]
    [InlineData("WaistEntries", "IWaistEntryRepository", "IWaistEntryReadRepository", "IWaistEntryReadModelRepository", "IWaistEntryWriteRepository")]
    [InlineData("Hydration", "IHydrationEntryReadModelRepository", "IHydrationEntryWriteRepository")]
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
    [InlineData("WeightEntryConfiguration.cs", "Modules/BodyMetrics/Infrastructure/Model/Configurations")]
    [InlineData("WaistEntryConfiguration.cs", "Modules/BodyMetrics/Infrastructure/Model/Configurations")]
    [InlineData("HydrationEntryConfiguration.cs", "Modules/Hydration/Infrastructure/Model/Configurations")]
    [InlineData("ExerciseEntryConfiguration.cs", "Modules/Exercises/Infrastructure/Model/Configurations/Exercises")]
    [InlineData("CycleProfileConfiguration.cs", "Modules/Cycles/Infrastructure/Model/Configurations")]
    [InlineData("CycleFactorConfiguration.cs", "Modules/Cycles/Infrastructure/Model/Configurations")]
    [InlineData("CycleSymptomEntryConfiguration.cs", "Modules/Cycles/Infrastructure/Model/Configurations")]
    [InlineData("BleedingEntryConfiguration.cs", "Modules/Cycles/Infrastructure/Model/Configurations")]
    [InlineData("FertilitySignalConfiguration.cs", "Modules/Cycles/Infrastructure/Model/Configurations")]
    public void HealthTrackingConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = expectedRelativeDirectory.StartsWith("Modules/", StringComparison.Ordinal)
            ? Path.Combine(
                ArchitectureTestPaths.RepositoryRoot,
                expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar),
                fileName)
            : Path.Combine(
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
    [InlineData("ShoppingListConfiguration.cs", "Modules/MealPlanning/Infrastructure/Model/Configurations/ShoppingLists")]
    [InlineData("ShoppingListItemConfiguration.cs", "Modules/MealPlanning/Infrastructure/Model/Configurations/ShoppingLists")]
    [InlineData("ShoppingListItemSourceConfiguration.cs", "Modules/MealPlanning/Infrastructure/Model/Configurations/ShoppingLists")]
    [InlineData("MealPlanConfiguration.cs", "Modules/MealPlanning/Infrastructure/Model/Configurations/MealPlans")]
    [InlineData("MealPlanDayConfiguration.cs", "Modules/MealPlanning/Infrastructure/Model/Configurations/MealPlans")]
    [InlineData("MealPlanMealConfiguration.cs", "Modules/MealPlanning/Infrastructure/Model/Configurations/MealPlans")]
    [InlineData("WearableConnectionConfiguration.cs", "Modules/Wearables/Infrastructure/Model/Configurations/Wearables")]
    [InlineData("WearableSyncEntryConfiguration.cs", "Modules/Wearables/Infrastructure/Model/Configurations/Wearables")]
    [InlineData("MarketingAttributionEventConfiguration.cs", "Modules/Marketing/Infrastructure/Model/Configurations")]
    public void PlanningWearablesAndMarketingConfigurations_StayInOwnedFolders(
        string fileName,
        string expectedRelativeDirectory) {
        string expectedPath = expectedRelativeDirectory.StartsWith("Modules/", StringComparison.Ordinal)
            ? Path.Combine(ArchitectureTestPaths.RepositoryRoot, expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar), fileName)
            : Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Infrastructure", "Persistence", expectedRelativeDirectory.Replace('/', Path.DirectorySeparatorChar), fileName);

        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in {expectedRelativeDirectory}.");
    }

    [Theory]
    [InlineData("Lessons", "INutritionLessonRepository", "INutritionLessonReadRepository", "INutritionLessonWriteRepository")]
    public void OtherApplicationModules_DoNotAcquireContentAggregateRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning content module administration or read capability");
    }

    [Theory]
    [InlineData("NutritionLessonConfiguration.cs")]
    [InlineData("UserLessonProgressConfiguration.cs")]
    public void LessonConfigurations_StayInLessonsPersistenceModel(string fileName) {
        string expectedPath = ArchitectureTestPaths.FromRoot(
            "Modules", "Lessons", "Infrastructure", "Model", "Configurations", fileName);
        Assert.True(File.Exists(expectedPath), $"{fileName} should stay in the Lessons persistence model.");
    }

    [Fact]
    public void DailyAdviceConfiguration_LivesInOwnedPersistenceModel() {
        string path = ArchitectureTestPaths.FromRoot(
            "Modules", "DailyAdvices", "Infrastructure", "Model", "Configurations", "DailyAdviceConfiguration.cs");
        Assert.True(File.Exists(path));
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
    [InlineData("ContentReports", "IContentReportWriteRepository")]
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
    [InlineData("Usda", "IUsdaFoodRepository", "IUsdaFoodReadRepository", "IUsdaFoodReadModelRepository")]
    public void OtherApplicationModules_DoNotAcquireExternalCatalogRepositories(
        string ownerModule,
        params string[] forbiddenContracts) {
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the external catalog read service or search-suggestion projection");
    }

    [Theory]
    [InlineData("Ai", "IAiUsageReadRepository", "IAiPromptTemplateReadModelRepository")]
    [InlineData("Lessons", "INutritionLessonReadModelRepository")]
    [InlineData("Email", "IEmailTemplateReadModelRepository")]
    [InlineData("ContentReports", "IContentReportReadModelRepository")]
    [InlineData("Authentication", "IUserLoginEventReadRepository")]
    [InlineData("Users", "IUserAdminReadModelRepository")]
    public void OtherApplicationModules_DoNotAcquireAdministrativeProjectionRepositories(
        string ownerModule,
        params string[] forbiddenContracts) =>
        AssertNoForeignRepositoryDependencies(
            ownerModule,
            forbiddenContracts,
            "use the owning module administration read capability");

    private static void AssertNoForeignRepositoryDependencies(
        string ownerModule,
        IReadOnlyCollection<string> forbiddenContracts,
        string guidance) {
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string ownerRoot = ModuleSourceCatalog.ApplicationRoot(ownerModule);
        string compositionRoot = Path.Combine(applicationRoot, "DependencyInjection.cs");

        string[] violations = [.. ModuleSourceCatalog.ApplicationFiles()
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

        return root.DescendantNodes()
            .OfType<NameSyntax>()
            .Where(name => name.Parent is not NameSyntax)
            .Select(name => new {
                Namespace = name.ToString(),
                Line = tree.GetLineSpan(name.Span).StartLinePosition.Line + 1,
            })
            .Where(entry => entry.Namespace.StartsWith(prefix, StringComparison.Ordinal))
            .DistinctBy(entry => (entry.Namespace, entry.Line))
            .Select(entry => new NamespaceDependency(path, entry.Line, entry.Namespace));
    }

    [ExcludeFromCodeCoverage]
    private sealed record NamespaceDependency(string Path, int Line, string Namespace);
}
