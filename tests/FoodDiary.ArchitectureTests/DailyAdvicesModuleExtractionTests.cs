namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DailyAdvicesModuleExtractionTests {
    [Fact]
    public void DailyAdvicesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "DailyAdvices");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.DailyAdvices");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedDailyAdvicesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.DailyAdvices/FoodDiary.Application.DailyAdvices.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterDailyAdvicesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddDailyAdvicesModule()", source, StringComparison.Ordinal);
    }
}
