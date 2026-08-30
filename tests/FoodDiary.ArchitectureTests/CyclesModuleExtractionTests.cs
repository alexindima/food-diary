namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CyclesModuleExtractionTests {
    [Fact]
    public void CyclesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Cycles");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedCyclesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Cycles/Application/FoodDiary.Application.Cycles.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Cycles.Application.Abstractions",
            "FoodDiary.Modules.Cycles.Domain",
        ], references);
    }

    [Fact]
    public void CyclesPersistenceOwnership_IsModuleOwnedWhileDbContextAndMigrationsStayCentral() {
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Infrastructure", "Model", "Configurations")));
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Infrastructure", "Persistence")));
        Assert.Empty(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Configurations", "Cycles"))
            ? SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Configurations", "Cycles")) : []);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs")));
        Assert.NotEmpty(Directory.GetFiles(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Migrations"), "*Cycle*.cs"));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterCyclesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddCyclesModule()", source, StringComparison.Ordinal);
    }
}
