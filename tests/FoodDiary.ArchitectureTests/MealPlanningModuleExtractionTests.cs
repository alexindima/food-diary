namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MealPlanningModuleExtractionTests {
    [Theory]
    [InlineData("MealPlans")]
    [InlineData("ShoppingLists")]
    public void MealPlanningApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.MealPlanning", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedMealPlanningAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application.MealPlanning", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedMealPlanningAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.MealPlanning/FoodDiary.Application.MealPlanning.csproj");
        string[] expectedReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterMealPlanningModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddMealPlanningModule()", source, StringComparison.Ordinal);
    }
}
