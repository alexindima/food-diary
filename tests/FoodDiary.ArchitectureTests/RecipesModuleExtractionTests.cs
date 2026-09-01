namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RecipesModuleExtractionTests {
    [Theory]
    [InlineData("Application/FoodDiary.Modules.Recipes.Application.csproj")]
    [InlineData("Application/Abstractions/FoodDiary.Modules.Recipes.Application.Abstractions.csproj")]
    [InlineData("Contracts/FoodDiary.Modules.Recipes.Contracts.csproj")]
    [InlineData("Infrastructure/FoodDiary.Modules.Recipes.Infrastructure.csproj")]
    [InlineData("Infrastructure/Model/FoodDiary.Modules.Recipes.PersistenceModel.csproj")]
    [InlineData("tests/FoodDiary.Modules.Recipes.Application.Tests/FoodDiary.Modules.Recipes.Application.Tests.csproj")]
    public void OwnedLayer_HasPhysicalProject(string relativePath) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Recipes", relativePath)));
    }

    [Theory]
    [InlineData("FoodDiary.Application.Recipes")]
    [InlineData("FoodDiary.Application.Abstractions/Recipes")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Recipes")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Configurations/Recipes")]
    public void DonorFolder_HasNoRemainingSources(string relativePath) {
        string path = ArchitectureTestPaths.FromRoot(relativePath);
        Assert.Empty(Directory.Exists(path) ? SourceScanner.SourceFiles(path) : []);
    }

    [Fact]
    public void CentralDomainCompatibilityGraph_IsPreserved() {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "Recipes", "Domain")));
        string user = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/Entities/Users/User.cs"));
        string recipe = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/Entities/Recipes/Recipe.cs"));
        string mealItem = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/Entities/Meals/MealItem.cs"));
        Assert.Contains("IReadOnlyCollection<Recipe> Recipes", user, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyCollection<MealItem> MealItems", recipe, StringComparison.Ordinal);
        Assert.Contains("Recipe? Recipe", mealItem, StringComparison.Ordinal);
        Assert.Contains("ApplyRecipeSnapshot(Recipe recipe)", mealItem, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Recipes", File.ReadAllText(
            ArchitectureTestPaths.FromRoot("FoodDiary.Domain/FoodDiary.Domain.csproj")), StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceModelAndTransactionBoundary_AreExplicit() {
        string context = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyRecipesPersistenceModel()", context, StringComparison.Ordinal);
        string infrastructure = ArchitectureTestPaths.FromRoot("Modules/Recipes/Infrastructure");
        string runner = Path.GetFullPath(Path.Combine(infrastructure, "Persistence", "Recipes", "EfRecipeMutationTransactionRunner.cs"));
        string runnerSource = File.ReadAllText(runner);
        Assert.Contains("RecipeCompositionTransactionLock.AcquireAsync", runnerSource, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync", runnerSource, StringComparison.Ordinal);
        Assert.DoesNotContain(SourceScanner.SourceFiles(infrastructure), path =>
            !string.Equals(Path.GetFullPath(path), runner, StringComparison.OrdinalIgnoreCase) &&
            File.ReadAllText(path).Contains("SaveChangesAsync(", StringComparison.Ordinal));
        Assert.DoesNotContain("FoodDiary.Modules.Recipes.Infrastructure", ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void RecipesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Recipes");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Recipes", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedRecipesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Recipes/Application/FoodDiary.Modules.Recipes.Application.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Application.Images",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.RecentItems.Application.Abstractions",
            "FoodDiary.Modules.Recipes.Application.Abstractions",
            "FoodDiary.Modules.Recipes.Contracts",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterRecipesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddRecipesModule()", source, StringComparison.Ordinal);
    }
}
