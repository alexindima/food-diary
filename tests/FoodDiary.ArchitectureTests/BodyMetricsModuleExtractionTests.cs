namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BodyMetricsModuleExtractionTests {
    [Theory]
    [InlineData("WeightEntries")]
    [InlineData("WaistEntries")]
    public void BodyMetricsApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.BodyMetrics", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.BodyMetrics",
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
            "FoodDiary.Application.BodyMetrics/FoodDiary.Application.BodyMetrics.csproj");
        string[] expectedReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterBodyMetricsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddBodyMetricsModule()", source, StringComparison.Ordinal);
    }
}
