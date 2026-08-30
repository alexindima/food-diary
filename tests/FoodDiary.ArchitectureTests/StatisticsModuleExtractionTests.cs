namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsModuleExtractionTests {
    [Fact]
    public void StatisticsApplicationSource_LivesOnlyInExtractedAssembly() {
        string originalLegacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Statistics");
        string extractedLegacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Statistics");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Statistics", "Application");
        Assert.Empty(Directory.Exists(originalLegacyRoot) ? SourceScanner.SourceFiles(originalLegacyRoot) : []);
        Assert.False(Directory.Exists(extractedLegacyRoot), $"Legacy project directory still exists: {extractedLegacyRoot}");
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedStatisticsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Statistics/Application/FoodDiary.Modules.Statistics.Application.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Fact]
    public void StatisticsApplicationAssembly_PreservesLegacyBinaryIdentity() {
        string project = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Statistics",
            "Application",
            "FoodDiary.Modules.Statistics.Application.csproj"));

        Assert.Contains("<AssemblyName>FoodDiary.Application.Statistics</AssemblyName>", project, StringComparison.Ordinal);
        Assert.Contains("<RootNamespace>FoodDiary.Application.Statistics</RootNamespace>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void StatisticsModule_DoesNotCreateUnownedLayers() {
        string moduleRoot = ArchitectureTestPaths.FromRoot("Modules", "Statistics");

        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Contracts")));
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Domain")));
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Infrastructure")));
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Application", "Abstractions")));
    }

    [Fact]
    public void StatisticsOwnedTests_LiveUnderLogicalModule() {
        string legacyTestsRoot = ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Application.Tests", "Statistics");
        string moduleTestsRoot = ArchitectureTestPaths.FromRoot(
            "Modules",
            "Statistics",
            "tests",
            "FoodDiary.Modules.Statistics.Application.Tests");

        Assert.Empty(Directory.Exists(legacyTestsRoot) ? SourceScanner.SourceFiles(legacyTestsRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(moduleTestsRoot));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterStatisticsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddStatisticsModule()", source, StringComparison.Ordinal);
    }
}
