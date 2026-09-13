namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class OwnedRuntimeContextBoundaryTests {
    [Theory]
    [InlineData("RecipeCommunity", "RecipeComments/RecipeCommentRepository.cs", "RecipeComment")]
    [InlineData("RecipeCommunity", "RecipeLikes/RecipeLikeRepository.cs", "RecipeLike")]
    [InlineData("MealPlanning", "ShoppingLists/ShoppingListRepository.cs", "ShoppingList")]
    public void RuntimeRepositoriesReceiveOnlyOwnedSets(string module, string file, string entity) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", module, "Infrastructure", "Persistence", file));
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
        Assert.Contains($"DbSet<{entity}>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MealPlanRepositoryUsesOwnedSetAndCompositionPort() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Infrastructure", "Persistence", "MealPlans", "MealPlanRepository.cs"));
        Assert.Contains("DbSet<MealPlan> plans", source, StringComparison.Ordinal);
        Assert.Contains("plans.Add(plan)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("context.Set<MealPlan>().Add", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
        Assert.Contains("IMealPlanCompositionReader", source, StringComparison.Ordinal);
    }
}
