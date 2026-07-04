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
            Path.Combine(persistenceRoot, "Billing", "EfBillingTransactionRunner.cs"),
            Path.Combine(persistenceRoot, "EfUnitOfWork.cs"),
            Path.Combine(persistenceRoot, "Images", "ImageObjectDeletionOutboxProcessor.cs"),
            Path.Combine(persistenceRoot, "Notifications", "NotificationWebPushOutboxProcessor.cs"),
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
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructureManualTransactionUsage_StaysInsideCurrentExplicitAllowlist() {
        string infrastructureRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure");
        string[] allowedFiles = [
            Path.Combine(infrastructureRoot, "Persistence", "Billing", "EfBillingTransactionRunner.cs"),
            Path.Combine(infrastructureRoot, "Services", "UserCleanupService.cs"),
        ];
        string[] forbiddenPatterns = [
            "BeginTransaction(",
            "BeginTransactionAsync(",
            "CommitAsync(",
            "RollbackAsync(",
        ];

        HashSet<string> allowed = allowedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] violations = [.. SourceScanner.SourceFiles(infrastructureRoot)
            .Where(path => !allowed.Contains(path))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line })
                .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
                .Select(entry => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{entry.index + 1}")))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
