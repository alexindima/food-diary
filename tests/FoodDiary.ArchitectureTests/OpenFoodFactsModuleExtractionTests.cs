namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class OpenFoodFactsModuleExtractionTests {
    [Fact]
    public void OpenFoodFactsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "OpenFoodFacts");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.OpenFoodFacts");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedOpenFoodFactsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.OpenFoodFacts/FoodDiary.Application.OpenFoodFacts.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterOpenFoodFactsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddOpenFoodFactsModule()", source, StringComparison.Ordinal);
    }
}
