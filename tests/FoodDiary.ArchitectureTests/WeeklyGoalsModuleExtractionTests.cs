namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalsModuleExtractionTests {
    [Fact]
    public void WeeklyGoalsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "WeeklyGoals");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedWeeklyGoalsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Meals.Contracts",
            "FoodDiary.Modules.WeeklyGoals.Application.Abstractions",
            "FoodDiary.Modules.WeeklyGoals.Contracts",
            "FoodDiary.Modules.WeeklyGoals.Domain",
        ], references);
    }

    [Fact]
    public void WeeklyGoalsDomainSource_LivesOnlyInExtractedAssembly() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "WeeklyGoals", "WeeklyGoal.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", "WeeklyGoalId.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", "WeeklyGoalType.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Domain", "Entities", "WeeklyGoals", "WeeklyGoal.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Domain", "ValueObjects", "Ids", "WeeklyGoalId.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Domain", "Enums", "WeeklyGoalType.cs")));
    }

    [Fact]
    public void ExtractedWeeklyGoalsDomain_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/WeeklyGoals/Domain/FoodDiary.Modules.WeeklyGoals.Domain.csproj");
        Assert.Equal(["FoodDiary.Domain"], references);
    }

    [Fact]
    public void WeeklyGoalsPersistence_LivesOnlyInModuleProjects() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "WeeklyGoals", "WeeklyGoalRepository.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Configurations", "WeeklyGoals", "WeeklyGoalConfiguration.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Infrastructure", "Persistence", "WeeklyGoalRepository.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "WeeklyGoals", "Infrastructure", "Model", "Configurations", "WeeklyGoalConfiguration.cs")));
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
