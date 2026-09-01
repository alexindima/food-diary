namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MealsModuleExtractionTests {
    [Fact]
    public void MealsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Meals");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Meals", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedMealsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Meals/Application/FoodDiary.Modules.Meals.Application.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Images",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Gamification.Application.Abstractions",
            "FoodDiary.Modules.Meals.Application.Abstractions",
            "FoodDiary.Modules.Meals.Contracts",
            "FoodDiary.Modules.RecentItems.Application.Abstractions",
        ], references);
    }

    [Fact]
    public void ExtractedMealsAssembly_PreservesLegacyClrIdentity() {
        string project = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "Meals", "Application", "FoodDiary.Modules.Meals.Application.csproj"));
        Assert.Contains("<AssemblyName>FoodDiary.Application.Meals</AssemblyName>", project, StringComparison.Ordinal);
        Assert.Contains("<RootNamespace>FoodDiary.Application.Meals</RootNamespace>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedDomainAndContext_RetainTheCentralRelationshipAndMigrationSeams() {
        string user = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Users", "User.cs"));
        string userConfiguration = ArchitectureTestPaths.FromRoot(
            "Modules", "Users", "Infrastructure", "Model", "Persistence", "Configurations", "Users", "UserConfiguration.cs");
        string dbContext = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs"));

        Assert.Contains("Meals", user, StringComparison.Ordinal);
        Assert.True(File.Exists(userConfiguration), "UserConfiguration must remain with the Users owner.");
        Assert.Contains("ApplyMealsPersistenceModel()", dbContext, StringComparison.Ordinal);
        Assert.True(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Migrations")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "Meals", "Domain")));
    }

    [Fact]
    public void MealsOwnedTests_LiveUnderTheModuleWithoutLegacyDuplicates() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "Meals", "tests", "FoodDiary.Modules.Meals.Application.Tests", "FoodDiary.Modules.Meals.Application.Tests.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "Meals", "tests", "FoodDiary.Modules.Meals.Domain.Tests", "FoodDiary.Modules.Meals.Domain.Tests.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "Meals", "tests", "FoodDiary.Modules.Meals.Infrastructure.IntegrationTests", "FoodDiary.Modules.Meals.Infrastructure.IntegrationTests.csproj")));
        string legacyTests = ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Application.Tests", "Meals");
        Assert.Empty(Directory.Exists(legacyTests) ? SourceScanner.SourceFiles(legacyTests) : []);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterMealsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddMealsModule()", source, StringComparison.Ordinal);
    }
}
