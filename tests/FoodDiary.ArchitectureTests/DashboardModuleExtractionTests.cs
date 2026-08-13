namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DashboardModuleExtractionTests {
    [Fact]
    public void DashboardApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Dashboard");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Dashboard");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedDashboardAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Dashboard/FoodDiary.Application.Dashboard.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Cycles",
            "FoodDiary.Application.DailyAdvices",
            "FoodDiary.Application.Exercises",
            "FoodDiary.Application.Hydration",
            "FoodDiary.Application.Meals",
            "FoodDiary.Application.Statistics",
            "FoodDiary.Application.Tdee",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterDashboardModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddDashboardModule()", source, StringComparison.Ordinal);
    }
}
