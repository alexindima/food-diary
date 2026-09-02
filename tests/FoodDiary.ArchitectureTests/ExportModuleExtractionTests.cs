namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ExportModuleExtractionTests {
    [Fact]
    public void ExportApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Export");
        string legacyAbstractionsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", "Export");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Export", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.Empty(Directory.Exists(legacyAbstractionsRoot) ? SourceScanner.SourceFiles(legacyAbstractionsRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedExportAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Cycles", "FoodDiary.Domain", "FoodDiary.Mediator", "FoodDiary.Modules.Export.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void ExportAbstractions_HaveOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Export/Application/Abstractions/FoodDiary.Modules.Export.Application.Abstractions.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterExportModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddExportModule()", source, StringComparison.Ordinal);
    }
}
