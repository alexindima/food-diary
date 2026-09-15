namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WearablesModuleBoundaryTests {
    [Fact]
    public void WearablesApplicationSource_DoesNotDependOnRootApplicationCommon() {
        string wearableRoot = ArchitectureTestPaths.FromRoot("Modules", "Wearables", "Application");
        string[] violations = [.. SourceScanner.SourceFiles(wearableRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .Where(entry => entry.line.Contains("FoodDiary.Application.Common", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OtherApplicationModules_DoNotReferenceWearablesImplementationNamespace() {
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");
        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .Where(entry => entry.line.Contains("FoodDiary.Modules.Wearables.Application", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void WearablesReadHandlers_SharePureSummaryCalculation() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Wearables", "Application");
        Assert.False(File.Exists(Path.Combine(root, "Common", "IWearableReadService.cs")));
        string calculator = File.ReadAllText(Path.Combine(root, "Common", "WearableSummaryCalculator.cs"));
        Assert.Contains("internal static class WearableSummaryCalculator", calculator, StringComparison.Ordinal);
        foreach (string path in new[] {
            Path.Combine(root, "Queries", "GetWearableDailySummary", "GetWearableDailySummaryQueryHandler.cs"),
            Path.Combine(root, "Commands", "SyncWearableData", "SyncWearableDataCommandHandler.cs"),
        }) {
            Assert.Contains("WearableSummaryCalculator.Calculate", File.ReadAllText(path), StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("GetWearableConnections", "GetWearableConnectionsQueryHandler.cs")]
    [InlineData("GetWearableDailySummary", "GetWearableDailySummaryQueryHandler.cs")]
    public void WearablesReadHandlers_RemainInternalToFeature(string query, string fileName) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "Wearables", "Application",
            "Queries",
            query,
            fileName));

        Assert.Contains("internal sealed class", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public sealed class", source, StringComparison.Ordinal);
    }
}
