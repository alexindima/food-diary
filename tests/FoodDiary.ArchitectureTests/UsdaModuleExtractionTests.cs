namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsdaModuleExtractionTests {
    [Fact]
    public void UsdaApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Usda");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Usda", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedUsdaAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Usda/Application/FoodDiary.Application.Usda.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void UsdaDomain_LivesOnlyInModuleProject() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Usda");
        string moduleRoot = ArchitectureTestPaths.FromRoot("Modules", "Usda", "Domain", "Entities", "Usda");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.Equal(6, SourceScanner.SourceFiles(moduleRoot).Count());

        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Usda/Domain/FoodDiary.Modules.Usda.Domain.csproj");
        Assert.Empty(references);
    }

    [Fact]
    public void UsdaPersistenceMappings_LiveOnlyInModuleProject() {
        string legacyRoot = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure", "Persistence", "Configurations", "Nutrition");
        string moduleRoot = ArchitectureTestPaths.FromRoot(
            "Modules", "Usda", "Infrastructure", "Model", "Configurations", "Usda");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.Equal(5, SourceScanner.SourceFiles(moduleRoot).Count());
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterUsdaModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddUsdaModule()", source, StringComparison.Ordinal);
    }
}
