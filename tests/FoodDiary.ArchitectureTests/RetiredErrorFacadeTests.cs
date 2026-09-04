namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RetiredErrorFacadeTests {
    [Theory]
    [InlineData("DailyAdvices", "DailyAdvice")]
    [InlineData("Fasting", "Fasting")]
    [InlineData("Hydration", "HydrationEntry")]
    [InlineData("BodyMetrics", "WeightEntry")]
    [InlineData("BodyMetrics", "WaistEntry")]
    [InlineData("Exercises", "Exercise")]
    [InlineData("Ai", "Ai")]
    [InlineData("Billing", "Billing")]
    [InlineData("Cycles", "Cycle")]
    [InlineData("Cycles", "CycleDay")]
    [InlineData("Dietologist", "Dietologist")]
    [InlineData("Favorites", "FavoriteMeal")]
    [InlineData("Favorites", "FavoriteProduct")]
    [InlineData("Favorites", "FavoriteRecipe")]
    [InlineData("Images", "Image")]
    [InlineData("Lessons", "Lesson")]
    [InlineData("Admin", "MailInbox")]
    [InlineData("Meals", "Meal")]
    [InlineData("MealPlanning", "MealPlan")]
    [InlineData("Products", "Product")]
    [InlineData("Recipes", "Recipe")]
    [InlineData("RecipeCommunity", "RecipeComment")]
    [InlineData("MealPlanning", "ShoppingList")]
    [InlineData("Usda", "Usda")]
    [InlineData("Users", "User")]
    [InlineData("Wearables", "Wearable")]
    public void CentralAbstractions_DoNotDeclareOrExportRetiredFacade(string module, string facade) {
        string central = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions");
        string[] declarations = [.. SourceScanner.SourceFiles(central)
            .SelectMany(CSharpSyntaxReader.ReadTypeDeclarations)
            .Where(type => string.Equals(type.Name, facade, StringComparison.Ordinal))
            .Select(type => type.Path)];

        Assert.Multiple(
            () => Assert.Empty(declarations),
            () => Assert.False(File.Exists(Path.Combine(central, "Common", "Abstractions", "Results", $"Errors.{facade}.cs"))),
            () => Assert.DoesNotContain($"FoodDiary.Modules.{module}.Application.Abstractions",
                ProjectReferenceReader.ReadProjectReferences("FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj"),
                StringComparer.Ordinal));
    }
}
