namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyCheckInModuleExtractionTests {
    [Fact]
    public void WeeklyCheckInApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.WeeklyCheckIn");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "WeeklyCheckIn", "Application");
        Assert.False(Directory.Exists(legacyRoot), $"Legacy project directory still exists: {legacyRoot}");
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedWeeklyCheckInAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/WeeklyCheckIn/Application/FoodDiary.Modules.WeeklyCheckIn.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void WeeklyCheckInModule_DoesNotCreateUnownedLayers() {
        string moduleRoot = ArchitectureTestPaths.FromRoot("Modules", "WeeklyCheckIn");
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Contracts")));
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Domain")));
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Infrastructure")));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterWeeklyCheckInModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddWeeklyCheckInModule()", source, StringComparison.Ordinal);
    }
}
