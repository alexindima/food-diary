namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WearablesModuleBoundaryTests {
    [Fact]
    public void WearablesApplicationSource_DoesNotDependOnRootApplicationCommon() {
        string wearableRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Wearables");
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
        string wearableRoot = Path.Combine(applicationRoot, "Wearables");
        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => !path.StartsWith(wearableRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, index }))
            .Where(entry => entry.line.Contains("FoodDiary.Application.Wearables", StringComparison.Ordinal))
            .Where(entry => !Path.GetFileName(entry.path).Equals("DependencyInjection.Wearables.cs", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void WearablesReadServiceContract_RemainsInternalToFeature() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Wearables",
            "Common",
            "IWearableReadService.cs"));

        Assert.Contains("internal interface IWearableReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public interface IWearableReadService", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GetWearableConnections", "GetWearableConnectionsQueryHandler.cs")]
    [InlineData("GetWearableDailySummary", "GetWearableDailySummaryQueryHandler.cs")]
    public void WearablesReadHandlers_RemainInternalToFeature(string query, string fileName) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Wearables",
            "Queries",
            query,
            fileName));

        Assert.Contains("internal sealed class", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public sealed class", source, StringComparison.Ordinal);
    }
}
