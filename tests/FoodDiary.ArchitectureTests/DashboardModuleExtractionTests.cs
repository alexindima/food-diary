namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DashboardModuleExtractionTests {
    [Fact]
    public void DashboardApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Dashboard");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules/Dashboard/Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Dashboard")));
    }

    [Fact]
    public void DashboardReadAdaptersAndPorts_HavePhysicalOwnersWithoutAggregateOwnership() {
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules/Dashboard/Infrastructure/Persistence/Dashboard")));
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules/Dashboard/Application/Abstractions")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules/Dashboard/Domain")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules/Dashboard/Infrastructure/Model")));
        Assert.DoesNotContain("FoodDiary.Modules.Dashboard.Infrastructure",
            ProjectReferenceReader.ReadProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj"), StringComparer.Ordinal);
        Assert.Equal(["FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Dashboard/Contracts/FoodDiary.Modules.Dashboard.Contracts.csproj"));
    }

    [Fact]
    public void ExtractedDashboardAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Dashboard/Application/FoodDiary.Modules.Dashboard.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Cycles.Contracts", "FoodDiary.Modules.DailyAdvices.Contracts", "FoodDiary.Modules.Dashboard.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Favorites.Domain.Contracts", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Meals.Domain.Contracts", "FoodDiary.Modules.Meals.Service.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Products.FoodQuality", "FoodDiary.Modules.Tdee.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterDashboardModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddDashboardModule()", source, StringComparison.Ordinal);
        Assert.Contains("AddDashboardReadServices()", source, StringComparison.Ordinal);
    }
}
