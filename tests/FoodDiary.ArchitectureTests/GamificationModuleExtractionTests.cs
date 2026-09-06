namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class GamificationModuleExtractionTests {
    [Fact]
    public void GamificationApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Gamification");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Gamification", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedGamificationAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Gamification/Application/FoodDiary.Modules.Gamification.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterGamificationModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddGamificationModule()", source, StringComparison.Ordinal);
    }
}
