using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class JobManagerGuardrailTests {
    [Fact]
    public void JobManagerProductionCode_UsesCancellationTokenNoneOnlyForHangfireRegistrationPlaceholders() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string jobManagerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager");

        string[] violations = [.. SourceScanner.SourceFiles(jobManagerRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("CancellationToken.None", StringComparison.Ordinal))
            .Where(static entry => !entry.line.Contains("Job.FromExpression", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
