namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BodyMetricsModuleExtractionTests {
    [Theory]
    [InlineData("WeightEntries")]
    [InlineData("WaistEntries")]
    public void BodyMetricsApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.BodyMetrics", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Application", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "BodyMetrics",
            "Application",
            "FoodDiary.Application.BodyMetrics.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedBodyMetricsAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.BodyMetrics", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedBodyMetricsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/BodyMetrics/Application/FoodDiary.Application.BodyMetrics.csproj");
        string[] expectedReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.BodyMetrics.Application.Abstractions",
        ];

        Assert.Equal(expectedReferences, references);
    }

    [Fact]
    public void BodyMetricsOwnedContracts_LiveInModuleAbstractions() {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", "WeightEntries")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", "WaistEntries")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "BodyMetrics", "Application", "Abstractions", "FoodDiary.Modules.BodyMetrics.Application.Abstractions.csproj")));
    }

    [Theory]
    [InlineData("WeightEntryConfiguration.cs")]
    [InlineData("WaistEntryConfiguration.cs")]
    public void BodyMetricsEntryConfigurations_LiveInModulePersistenceModel(string fileName) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "BodyMetrics", "Infrastructure", "Model", "Configurations", fileName)));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterBodyMetricsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddBodyMetricsModule()", source, StringComparison.Ordinal);
    }
}
