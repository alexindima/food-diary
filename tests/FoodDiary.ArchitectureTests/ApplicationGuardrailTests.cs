using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        string[] violations = [.. GetFilesIfDirectoryExists(servicesRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => !signature.Contains("CancellationToken", StringComparison.Ordinal))
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationPersistenceInterfaces_AsyncMethodsAcceptCancellationToken() {
        string root = GetRepositoryRoot();
        string persistenceRoot = Path.Combine(root, "FoodDiary.Application.Abstractions", "Common", "Interfaces", "Persistence");

        string[] violations = [.. Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => !signature.Contains("CancellationToken", StringComparison.Ordinal))
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))];

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

        var actualFiles = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
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
        string[] allowedFiles = [
            "IUserRepository.cs",
            "ProductQueryFilters.cs",
            "RecipeQueryFilters.cs",
            "UserAccountStatusFilter.cs",
        ];

        string?[] actualFiles = [.. Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.TopDirectoryOnly)
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
            "Common",
            "Interfaces",
            "Persistence",
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
    public void RecipeNutritionUpdater_DoesNotUseFullRecipeRepository() {
        string root = GetRepositoryRoot();
        string updaterPath = Path.Combine(root, "FoodDiary.Application", "Recipes", "Services", "RecipeNutritionUpdater.cs");

        string[] violations = FindReferencesInFiles(root, [updaterPath], "IRecipeRepository");

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
    public void ApplicationSourceFiles_UseFullUserRepositoryOnlyOutsideMigratedUserSlices() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedDirectories = [
            Path.Combine(applicationRoot, "Ai"),
            Path.Combine(applicationRoot, "Consumptions"),
            Path.Combine(applicationRoot, "Cycles"),
            Path.Combine(applicationRoot, "DailyAdvices"),
            Path.Combine(applicationRoot, "Dashboard"),
            Path.Combine(applicationRoot, "Export"),
            Path.Combine(applicationRoot, "FavoriteProducts"),
            Path.Combine(applicationRoot, "FavoriteRecipes"),
            Path.Combine(applicationRoot, "FavoriteMeals"),
            Path.Combine(applicationRoot, "Fasting"),
            Path.Combine(applicationRoot, "Gamification"),
            Path.Combine(applicationRoot, "Hydration"),
            Path.Combine(applicationRoot, "Products"),
            Path.Combine(applicationRoot, "Recipes"),
            Path.Combine(applicationRoot, "ShoppingLists"),
            Path.Combine(applicationRoot, "Statistics"),
            Path.Combine(applicationRoot, "Tdee"),
            Path.Combine(applicationRoot, "WaistEntries"),
            Path.Combine(applicationRoot, "WeeklyCheckIn"),
            Path.Combine(applicationRoot, "WeightEntries"),
        ];

        string[] violations = [.. migratedDirectories.SelectMany(directory =>
            FindRepositoryReferenceViolations(
                root,
                directory,
                "IUserRepository",
                []))];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAdminReadHandlers_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminDashboardSummary", "GetAdminDashboardSummaryQueryHandler.cs"),
            Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminUser", "GetAdminUserQueryHandler.cs"),
            Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminUserRoleAudit", "GetAdminUserRoleAuditQueryHandler.cs"),
            Path.Combine(applicationRoot, "Admin", "Queries", "GetAdminUsers", "GetAdminUsersQueryHandler.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "IUserRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAdminUserCommandHandlers_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Admin", "Commands", "StartAdminImpersonation", "StartAdminImpersonationCommandHandler.cs"),
            Path.Combine(applicationRoot, "Admin", "Commands", "UpdateAdminUser", "UpdateAdminUserCommandHandler.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "IUserRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void AdminSlice_UsesUserRepositoryOnlyThroughFocusedAdapterServices() {
        string root = GetRepositoryRoot();
        string adminRoot = Path.Combine(root, "FoodDiary.Application", "Admin");
        string[] allowedFiles = [
            Path.Combine(adminRoot, "Services", "AdminImpersonationUserService.cs"),
            Path.Combine(adminRoot, "Services", "AdminUserManagementService.cs"),
            Path.Combine(adminRoot, "Services", "AdminUserReadService.cs"),
        ];

        string[] adminFiles = [.. SourceScanner.SourceFiles(adminRoot)
            .Where(path => !allowedFiles.Contains(path, StringComparer.OrdinalIgnoreCase))];

        string[] violations = FindReferencesInFiles(root, adminFiles, "IUserRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAuthenticationLookupHandlers_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Authentication", "Commands", "AdminSsoExchange", "AdminSsoExchangeCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "AdminSsoStart", "AdminSsoStartCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "Login", "LoginCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "RefreshToken", "RefreshTokenCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "TelegramBotAuth", "TelegramBotAuthCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "TelegramLoginWidget", "TelegramLoginWidgetCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "TelegramVerify", "TelegramVerifyCommandHandler.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "IUserRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAuthenticationMutationServices_DoNotUseFullUserRepositoryOutsideAdapter() {
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

        string[] violations = [
            .. FindReferencesInFiles(root, migratedFiles, "IUserRepository"),
            .. FindReferencesInFiles(root, migratedFiles, "CurrentUserAccessPolicy"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedAuthenticationRegistrationServices_DoNotUseFullUserRepositoryOutsideAdapter() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Authentication", "Commands", "BootstrapInitialAdmin", "BootstrapInitialAdminCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "Register", "RegisterCommandHandler.cs"),
            Path.Combine(applicationRoot, "Authentication", "Commands", "Register", "RegisterCommandValidator.cs"),
        ];

        string[] violations = FindReferencesInFiles(root, migratedFiles, "IUserRepository");

        Assert.Empty(violations);
    }

    [Fact]
    public void AuthenticationSlice_UsesUserRepositoryOnlyThroughFocusedAdapterServices() {
        string root = GetRepositoryRoot();
        string authenticationRoot = Path.Combine(root, "FoodDiary.Application", "Authentication");
        string[] allowedFiles = [
            Path.Combine(authenticationRoot, "Services", "AuthenticationUserLookupService.cs"),
            Path.Combine(authenticationRoot, "Services", "AuthenticationUserMutationService.cs"),
            Path.Combine(authenticationRoot, "Services", "AuthenticationUserRegistrationService.cs"),
        ];

        string[] authenticationFiles = [.. SourceScanner.SourceFiles(authenticationRoot)
            .Where(path => !allowedFiles.Contains(path, StringComparer.OrdinalIgnoreCase))];

        string[] violations = FindReferencesInFiles(root, authenticationFiles, "IUserRepository");

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

        Assert.False(File.Exists(servicePath), "UserContextService should remain the single IUserRepository-backed current-user access implementation.");
    }

    [Fact]
    public void MigratedNotificationHandlers_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Notifications", "Commands", "MarkAllNotificationsRead", "MarkAllNotificationsReadCommandHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Commands", "MarkNotificationRead", "MarkNotificationReadCommandHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Commands", "RemoveWebPushSubscription", "RemoveWebPushSubscriptionCommandHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Commands", "ScheduleTestNotification", "ScheduleTestNotificationCommandHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Commands", "UpdateNotificationPreferences", "UpdateNotificationPreferencesCommandHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Commands", "UpsertWebPushSubscription", "UpsertWebPushSubscriptionCommandHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Queries", "GetNotificationPreferences", "GetNotificationPreferencesQueryHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Queries", "GetNotifications", "GetNotificationsQueryHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Queries", "GetUnreadCount", "GetUnreadCountQueryHandler.cs"),
            Path.Combine(applicationRoot, "Notifications", "Queries", "GetWebPushSubscriptions", "GetWebPushSubscriptionsQueryHandler.cs"),
        ];

        string[] violations = [.. migratedFiles
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => entry.line.Contains("IUserRepository", StringComparison.Ordinal))
                .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")))];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedDietologistAccessOnlyHandlers_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Dietologist", "Commands", "AcceptInvitation", "AcceptInvitationCommandHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Commands", "AcceptInvitationForCurrentUser", "AcceptInvitationForCurrentUserCommandHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Commands", "DisconnectDietologist", "DisconnectDietologistCommandHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Commands", "MarkRecommendationRead", "MarkRecommendationReadCommandHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Commands", "RevokeInvitation", "RevokeInvitationCommandHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Commands", "UpdateDietologistPermissions", "UpdateDietologistPermissionsCommandHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "EventHandlers", "RecommendationCreatedEventHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetClientDashboard", "GetClientDashboardQueryHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetInvitationByToken", "GetInvitationByTokenQueryHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetMyClients", "GetMyClientsQueryHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetMyDietologist", "GetMyDietologistQueryHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetMyDietologistRelationship", "GetMyDietologistRelationshipQueryHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetMyRecommendations", "GetMyRecommendationsQueryHandler.cs"),
            Path.Combine(applicationRoot, "Dietologist", "Queries", "GetRecommendationsForClient", "GetRecommendationsForClientQueryHandler.cs"),
        ];

        string[] violations = [.. migratedFiles
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => entry.line.Contains("IUserRepository", StringComparison.Ordinal))
                .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")))];

        Assert.Empty(violations);
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
    public void MigratedBillingUserContextHandlers_DoNotUseFullUserRepository() {
        string root = GetRepositoryRoot();
        string applicationRoot = Path.Combine(root, "FoodDiary.Application");
        string[] migratedFiles = [
            Path.Combine(applicationRoot, "Billing", "Commands", "CreateCheckoutSession", "CreateCheckoutSessionCommandHandler.cs"),
            Path.Combine(applicationRoot, "Billing", "Commands", "CreatePortalSession", "CreatePortalSessionCommandHandler.cs"),
            Path.Combine(applicationRoot, "Billing", "Commands", "StartPremiumTrial", "StartPremiumTrialCommandHandler.cs"),
            Path.Combine(applicationRoot, "Billing", "Queries", "GetBillingOverview", "GetBillingOverviewQueryHandler.cs"),
        ];

        string[] violations = [.. migratedFiles
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => entry.line.Contains("IUserRepository", StringComparison.Ordinal))
                .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")))];

        Assert.Empty(violations);
    }

    [Fact]
    public void BillingSlice_UsesUserRepositoryOnlyThroughBillingUserLookupService() {
        string root = GetRepositoryRoot();
        string billingRoot = Path.Combine(root, "FoodDiary.Application", "Billing");
        string allowedPath = Path.Combine(billingRoot, "Services", "BillingUserLookupService.cs");
        string[] billingFiles = [.. SourceScanner.SourceFiles(billingRoot)
            .Where(path => !string.Equals(path, allowedPath, StringComparison.OrdinalIgnoreCase))];

        string[] violations = [
            .. FindReferencesInFiles(root, billingFiles, "IUserRepository"),
            .. FindReferencesInFiles(root, billingFiles, "CurrentUserAccessPolicy"),
        ];

        Assert.Empty(violations);
    }

    [Fact]
    public void NotificationsSlice_UsesUserRepositoryOnlyThroughNotificationUserAccessService() {
        string root = GetRepositoryRoot();
        string notificationsRoot = Path.Combine(root, "FoodDiary.Application", "Notifications");
        string[] notificationFiles = [.. SourceScanner.SourceFiles(notificationsRoot)];

        string[] violations = [
            .. FindReferencesInFiles(root, notificationFiles, "IUserRepository"),
            .. FindReferencesInFiles(root, notificationFiles, "CurrentUserAccessPolicy"),
        ];

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
    public void UserProfileFeatureSlices_UseUserRepositoryOnlyThroughDedicatedProfileServices() {
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

            return FindReferencesInFiles(root, files, "IUserRepository")
                .Concat(FindReferencesInFiles(root, files, "CurrentUserAccessPolicy"));
        })];

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedUserHandlers_DoNotUseFullUserRepositoryOrAccessPolicy() {
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

        string[] violations = [
            .. FindReferencesInFiles(root, migratedFiles, "IUserRepository"),
            .. FindReferencesInFiles(root, migratedFiles, "CurrentUserAccessPolicy"),
        ];

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

        string repositoryRegistrationPath = Path.Combine(root, "FoodDiary.Infrastructure", "DependencyInjection.Repositories.cs");
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
