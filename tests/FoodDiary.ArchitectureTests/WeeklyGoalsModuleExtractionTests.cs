namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalsModuleExtractionTests {
    [Fact]
    public void WeeklyGoalsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "WeeklyGoals");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.WeeklyGoals");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedWeeklyGoalsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.WeeklyGoals/FoodDiary.Application.WeeklyGoals.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Meals",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterWeeklyGoalsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddWeeklyGoalsModule()", source, StringComparison.Ordinal);
    }
}
