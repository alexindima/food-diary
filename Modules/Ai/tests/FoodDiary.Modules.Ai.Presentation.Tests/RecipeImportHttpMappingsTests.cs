using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Presentation.Recipes.Mappings;
using FoodDiary.Modules.Ai.Presentation.Responses;

namespace FoodDiary.Modules.Ai.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeImportHttpMappingsTests {
    [Fact]
    public void ImportResponse_PreservesStepAssignmentsAndUnscaledAuthorNutrition() {
        var draft = new RecipeImportDraftModel("Chicken", Description: null,
            [new("Pepper", "1/2 tsp", 1), new("Pepper", "1/2 tsp", 3)],
            ["Preheat", "Mix seasoning", "Season", "Mix gravy"], 5, 5, 50,
            "505 kcal per serving", "https://example.org/recipe",
            new RecipeImportNutritionModel("serving", 505, 36, 37, 5, 1, Alcohol: null));

        RecipeImportHttpResponse response = draft.ToHttpResponse();

        Assert.Equal([1, 3], response.Ingredients.Select(x => x.StepIndex));
        Assert.Equal(new RecipeImportNutritionHttpResponse("serving", 505, 36, 37, 5, 1, Alcohol: null), response.Nutrition);
        Assert.Equal(draft.AuthorNutrition, response.AuthorNutrition);
        Assert.Equal(draft.SourceUrl, response.SourceUrl);
    }

    [Fact]
    public void LegacyDraft_MapsWithoutStructuredNutritionOrStepAssignments() {
        var draft = new RecipeImportDraftModel("Salad", Description: null, [new("Salt", "pinch")], ["Mix"],
            Servings: null, PrepMinutes: null, CookMinutes: null, AuthorNutrition: null, SourceUrl: null);

        RecipeImportHttpResponse response = draft.ToHttpResponse();

        Assert.Null(response.Nutrition);
        Assert.Null(response.Ingredients[0].StepIndex);
    }
}
