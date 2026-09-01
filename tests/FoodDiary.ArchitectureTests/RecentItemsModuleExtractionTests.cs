namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RecentItemsModuleExtractionTests {
    [Fact]
    public void RecentItemsOwnerSource_LivesOnlyInModule() {
        string[] legacyPaths = [
            ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", "RecentItems"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Recents"),
            ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "RecentItems"),
        ];
        Assert.All(legacyPaths, path => Assert.Empty(Directory.Exists(path) ? SourceScanner.SourceFiles(path) : []));
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", "RecentItems")));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    public void CompositionRoots_RegisterRecentItemsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddRecentItemsModule()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedContext_AppliesRecentItemsPersistenceModel() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyRecentItemsPersistenceModel()", source, StringComparison.Ordinal);
    }
}
