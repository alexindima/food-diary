namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyCheckInModuleExtractionTests {
    [Fact]
    public void WeeklyCheckInApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "WeeklyCheckIn");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.WeeklyCheckIn");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedWeeklyCheckInAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.WeeklyCheckIn/FoodDiary.Application.WeeklyCheckIn.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Hydration",
            "FoodDiary.Application.Meals",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterWeeklyCheckInModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddWeeklyCheckInModule()", source, StringComparison.Ordinal);
    }
}
