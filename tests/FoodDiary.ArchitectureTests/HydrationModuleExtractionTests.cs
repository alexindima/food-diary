namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HydrationModuleExtractionTests {
    [Fact]
    public void HydrationApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Hydration");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Hydration");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedHydrationAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Hydration/FoodDiary.Application.Hydration.csproj");
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
    public void ExecutableCompositionRoots_RegisterHydrationModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddHydrationModule()", source, StringComparison.Ordinal);
    }
}
