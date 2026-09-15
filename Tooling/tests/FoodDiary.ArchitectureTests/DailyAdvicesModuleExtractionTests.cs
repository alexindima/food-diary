namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DailyAdvicesModuleExtractionTests {
    [Fact]
    public void DailyAdvicesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "DailyAdvices");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "DailyAdvices", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void DailyAdvicesOwnedLayers_ArePhysicalModuleProjects() {
        string[] projects = [
            "Application.Abstractions/FoodDiary.Modules.DailyAdvices.Application.Abstractions.csproj",
            "Domain/FoodDiary.Modules.DailyAdvices.Domain.csproj",
            "PersistenceModel/FoodDiary.Modules.DailyAdvices.PersistenceModel.csproj",
            "Infrastructure/FoodDiary.Modules.DailyAdvices.Infrastructure.csproj",
        ];
        Assert.All(projects, project => Assert.True(File.Exists(Path.Combine(
            ArchitectureTestPaths.RepositoryRoot,
            "Modules",
            "DailyAdvices",
            project.Replace('/', Path.DirectorySeparatorChar))), project));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterDailyAdvicesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddDailyAdvicesModule()", source, StringComparison.Ordinal);
    }
}
