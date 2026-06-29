using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PersistenceTransactionGuardrailTests {
    [Fact]
    public void PersistenceSaveChangesAsyncUsage_StaysInsideCurrentExplicitAllowlist() {
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] allowedFiles = [
            Path.Combine(persistenceRoot, "Ai", "AiUsageRepository.cs"),
            Path.Combine(persistenceRoot, "Billing", "BillingPaymentRepository.cs"),
            Path.Combine(persistenceRoot, "Billing", "BillingSubscriptionRepository.cs"),
            Path.Combine(persistenceRoot, "Billing", "BillingWebhookEventRepository.cs"),
            Path.Combine(persistenceRoot, "EfUnitOfWork.cs"),
            Path.Combine(persistenceRoot, "Images", "ImageAssetRepository.cs"),
            Path.Combine(persistenceRoot, "Notifications", "NotificationRepository.cs"),
            Path.Combine(persistenceRoot, "Notifications", "WebPushSubscriptionRepository.cs"),
            Path.Combine(persistenceRoot, "OpenFoodFacts", "OpenFoodFactsProductCacheRepository.cs"),
            Path.Combine(persistenceRoot, "RecentItems", "RecentItemRepository.cs"),
            Path.Combine(persistenceRoot, "Tracking", "FastingSessionRepository.cs"),
            Path.Combine(persistenceRoot, "Tracking", "FastingTelemetryEventRepository.cs"),
        ];

        HashSet<string> allowed = allowedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] violations = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(path => !allowed.Contains(path))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains("SaveChangesAsync(", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}"))
            .OrderBy(static value => value, StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
