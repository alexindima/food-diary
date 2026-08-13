namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CyclesModuleExtractionTests {
    [Fact]
    public void CyclesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Cycles");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Cycles");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedCyclesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Cycles/FoodDiary.Application.Cycles.csproj");
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
    public void ExecutableCompositionRoots_RegisterCyclesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddCyclesModule()", source, StringComparison.Ordinal);
    }
}
