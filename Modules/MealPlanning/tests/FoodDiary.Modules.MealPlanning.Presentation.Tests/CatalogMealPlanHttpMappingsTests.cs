using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;

namespace FoodDiary.Modules.MealPlanning.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class CatalogMealPlanHttpMappingsTests {
    [Fact]
    public async Task NullNestedChildren_AreRejectedByValidationAfterTransportMapping() {
        SaveCatalogMealPlanHttpRequest request = new(
            Name: "Invalid nested catalog entries", Description: null, DietType: "Balanced", DurationDays: 1,
            TargetCaloriesPerDay: null, IsPublished: false,
            Days: [null!, new CatalogDayHttpRequest(1, [null!])]);
        SaveCatalogMealPlanCommand command = request.ToCatalogCommand(id: null);

        FluentValidation.Results.ValidationResult result = await new SaveCatalogMealPlanCommandValidator().ValidateAsync(command);

        Assert.Multiple(
            () => Assert.False(result.IsValid),
            () => Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Days[0].DayNumber", StringComparison.Ordinal)),
            () => Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Days[1].Meals[0].RecipeId", StringComparison.Ordinal)),
            () => Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Days[1].Meals[0].Servings", StringComparison.Ordinal)));
    }
}
