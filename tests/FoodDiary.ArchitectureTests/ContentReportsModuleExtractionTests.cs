namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportsModuleExtractionTests {
    [Fact]
    public void ContentReportsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "ContentReports");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "ContentReports", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedContentReportsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/ContentReports/Application/FoodDiary.Modules.ContentReports.Application.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.ContentReports.Application.Abstractions",
            "FoodDiary.Modules.ContentReports.Contracts",
            "FoodDiary.Modules.ContentReports.Domain",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterContentReportsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddContentReportsModule()", source, StringComparison.Ordinal);
    }
}
