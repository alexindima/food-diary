namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ExportModuleExtractionTests {
    [Fact]
    public void ExportApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Export");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Export");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedExportAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Export/FoodDiary.Application.Export.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Cycles",
            "FoodDiary.Application.Meals",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterExportModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddExportModule()", source, StringComparison.Ordinal);
    }
}
