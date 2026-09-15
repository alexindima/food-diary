using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PersistenceTransactionGuardrailTests {
    [Fact]
    public void PersistenceRepositories_UseTimeProviderInsteadOfDirectUtcNow() {
        string[] persistenceRoots = [
            ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence"),
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(persistenceRoots, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PersistenceSaveChangesAsyncUsage_StaysInsideCurrentExplicitAllowlist() {
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] allowedFiles = [
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "FoodRecognitionJobStore.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Admin", "Infrastructure", "Integrations", "MailInbox", "BugAcknowledgementReceipts.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "AiQuotaRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "AiDbContext.cs"),
            // Framework save override translates exact provider conflicts; the shared unit of work still owns completion.
            ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Infrastructure", "Persistence", "BodyMetricsDbContext.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "ContentReports", "Infrastructure", "Persistence", "ContentReportsDbContext.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "ContentReports", "Infrastructure", "Persistence", "ContentReportsDbContext.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Images", "Infrastructure", "Persistence", "Images", "ImageAssetCleanupBatch.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "EfUnitOfWork.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "SharedPersistenceDbContext.Session.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Shared", "ModuleContextSaveCoordinator.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Shared", "EfModuleSessionCoordinator.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Meals", "Infrastructure", "Persistence", "Meals", "EfMealRecognitionTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Outbox.Infrastructure", "Persistence", "OutboxProcessingEngine.cs"),
            ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Outbox.Infrastructure", "Persistence", "OutboxMessageClaimer.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Outbox", "OutboxDeadLetterReplayService.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "RecentItems", "Infrastructure", "Persistence", "RecentItems", "PostCommitRecentItemUsageRecorder.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Shared", "EfModuleTransactionCoordinator.cs"),
        ];

        HashSet<string> allowed = allowedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] violations = [.. ModuleSourceCatalog.InfrastructureFiles()
            .Where(path => !allowed.Contains(path))
            .SelectMany(path => SourceScanner.ReadCodeLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains("SaveChangesAsync(", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructureManualTransactionUsage_StaysInsideCurrentExplicitAllowlist() {
        string infrastructureRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure");
        string[] allowedFiles = [
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "FoodRecognitionJobStore.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "AiQuotaRepository.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Shared", "ModuleContextSaveCoordinator.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Outbox", "OutboxDeadLetterReplayService.cs"),
            ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Outbox.Infrastructure", "Persistence", "OutboxMessageClaimer.cs"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime", "Persistence", "Shared", "EfModuleTransactionCoordinator.cs"),
        ];
        string[] forbiddenPatterns = [
            "BeginTransaction(",
            "BeginTransactionAsync(",
            "CommitAsync(",
            "RollbackAsync(",
        ];

        HashSet<string> allowed = allowedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] violations = [.. ModuleSourceCatalog.InfrastructureFiles()
            .Where(path => !allowed.Contains(path))
            .SelectMany(path => SourceScanner.ReadCodeLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructureBulkMutationUsage_StaysInsideCurrentExplicitAllowlist() {
        string infrastructureRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure");
        string identityLoginEvents = ArchitectureTestPaths.FromRoot("Modules", "Identity", "Infrastructure", "Persistence", "Users", "UserLoginEventRepository.cs");
        string[] allowedFiles = [
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "FoodRecognitionJobStore.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Notifications", "Infrastructure", "Persistence", "NotificationRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Fasting", "Infrastructure", "Persistence", "FastingTelemetryEventRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Marketing", "Infrastructure", "Persistence", "MarketingAttributionEventRepository.cs"),
            identityLoginEvents,
            ArchitectureTestPaths.FromRoot("Modules", "Identity", "Infrastructure", "Persistence", "Authentication", "TelegramLoginTicketStore.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Identity", "Infrastructure", "Persistence", "Authentication", "TelegramOperationStore.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Identity", "Infrastructure", "Persistence", "Authentication", "IdentityUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Identity", "Infrastructure", "Persistence", "Users", "RefreshTokenSessionRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Users", "Infrastructure", "Persistence", "Users", "UserCleanupService.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Admin", "Infrastructure", "Persistence", "AdminUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "AiUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Infrastructure", "Persistence", "BodyMetricsUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Infrastructure", "Persistence", "CyclesUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Dietologist", "Infrastructure", "Persistence", "DietologistUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Infrastructure", "Persistence", "HydrationUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Images", "Infrastructure", "Persistence", "Images", "ImageAssetOwnershipService.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Images", "Infrastructure", "Persistence", "ImagesUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Infrastructure", "Persistence", "MealPlanningUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Meals", "Infrastructure", "Persistence", "MealsUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Products", "Infrastructure", "Persistence", "ProductsUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "RecentItems", "Infrastructure", "Persistence", "RecentItemsUserDataPurgeParticipant.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Recipes", "Infrastructure", "Persistence", "RecipesUserDataPurgeParticipant.cs"),
        ];
        string[] forbiddenPatterns = [
            "ExecuteDeleteAsync(",
            "ExecuteUpdateAsync(",
        ];

        HashSet<string> allowed = allowedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] violations = [.. ModuleSourceCatalog.InfrastructureFiles()
            .Where(path => !allowed.Contains(path))
            .SelectMany(path => SourceScanner.ReadCodeLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
