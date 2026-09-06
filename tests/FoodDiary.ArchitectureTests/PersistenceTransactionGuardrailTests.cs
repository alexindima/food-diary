using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PersistenceTransactionGuardrailTests {
    [Fact]
    public void PersistenceRepositories_UseTimeProviderInsteadOfDirectUtcNow() {
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");

        string[] violations = SourceScanner.FindLinePatternViolations(persistenceRoot, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PersistenceSaveChangesAsyncUsage_StaysInsideCurrentExplicitAllowlist() {
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] allowedFiles = [
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "Ai", "AiQuotaRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Billing", "Infrastructure", "Persistence", "EfBillingTransactionRunner.cs"),
            Path.Combine(persistenceRoot, "EfUnitOfWork.cs"),
            Path.Combine(persistenceRoot, "Outbox", "OutboxProcessingEngine.cs"),
            Path.Combine(persistenceRoot, "Outbox", "OutboxDeadLetterReplayService.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Products", "Infrastructure", "Persistence", "Products", "EfProductMutationTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Recipes", "Infrastructure", "Persistence", "Recipes", "EfRecipeMutationTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "RecentItems", "Infrastructure", "Persistence", "RecentItems", "PostCommitRecentItemUsageRecorder.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Users", "Infrastructure", "Persistence", "Users", "UserCleanupService.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Wearables", "Infrastructure", "Persistence", "EfWearableTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Infrastructure", "Persistence", "EfWeeklyGoalTransactionRunner.cs"),
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
            ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "Ai", "AiQuotaRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Billing", "Infrastructure", "Persistence", "EfBillingTransactionRunner.cs"),
            Path.Combine(infrastructureRoot, "Persistence", "Outbox", "OutboxDeadLetterReplayService.cs"),
            Path.Combine(infrastructureRoot, "Persistence", "Outbox", "OutboxMessageClaimer.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Products", "Infrastructure", "Persistence", "Products", "EfProductMutationTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Recipes", "Infrastructure", "Persistence", "Recipes", "EfRecipeMutationTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Users", "Infrastructure", "Persistence", "Users", "UserCleanupService.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Wearables", "Infrastructure", "Persistence", "EfWearableTransactionRunner.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Infrastructure", "Persistence", "EfWeeklyGoalTransactionRunner.cs"),
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
            ArchitectureTestPaths.FromRoot("Modules", "Notifications", "Infrastructure", "Persistence", "NotificationRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Fasting", "Infrastructure", "Persistence", "FastingTelemetryEventRepository.cs"),
            ArchitectureTestPaths.FromRoot("Modules", "Marketing", "Infrastructure", "Persistence", "MarketingAttributionEventRepository.cs"),
            identityLoginEvents,
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
