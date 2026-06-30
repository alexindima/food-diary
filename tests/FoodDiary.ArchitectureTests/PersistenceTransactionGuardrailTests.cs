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
