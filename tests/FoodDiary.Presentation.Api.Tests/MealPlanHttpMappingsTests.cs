using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Presentation.Api.Features.MealPlans.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class MealPlanHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsUserIdAndDietType() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery("LowCarb");

        Assert.Equal(userId, query.UserId);
        Assert.Equal("LowCarb", query.DietType);
    }

    [Fact]
    public void ToGetByIdQuery_MapsIds() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var query = userId.ToGetByIdQuery(planId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(planId, query.PlanId);
    }

    [Fact]
    public void ToAdoptCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var command = userId.ToAdoptCommand(planId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(planId, command.PlanId);
    }

    [Fact]
    public void ToGenerateShoppingListCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var command = userId.ToGenerateShoppingListCommand(planId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(planId, command.PlanId);
    }

    [Fact]
    public void MealPlanSummaryModelList_ToHttpResponse_MapsAllItems() {
        var models = new List<MealPlanSummaryModel> {
            new(Guid.NewGuid(), "Balanced", "Description", "Balanced", 7, 2200, true, 12),
            new(Guid.NewGuid(), "Low carb", null, "LowCarb", 5, null, false, 8),
        };

        var responses = models.ToHttpResponse();

        Assert.Equal(2, responses.Count);
        Assert.Equal(models[0].Id, responses[0].Id);
        Assert.Equal("Balanced", responses[0].Name);
        Assert.Equal("Description", responses[0].Description);
        Assert.Equal("Balanced", responses[0].DietType);
        Assert.Equal(7, responses[0].DurationDays);
        Assert.Equal(2200, responses[0].TargetCaloriesPerDay);
        Assert.True(responses[0].IsCurated);
        Assert.Equal(12, responses[0].TotalRecipes);
        Assert.Null(responses[1].TargetCaloriesPerDay);
    }

    [Fact]
    public void MealPlanModel_ToHttpResponse_MapsNestedDaysAndMeals() {
        var mealId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var model = new MealPlanModel(
            Id: Guid.NewGuid(),
            Name: "Plan",
            Description: null,
            DietType: "Balanced",
            DurationDays: 1,
            TargetCaloriesPerDay: 2100,
            IsCurated: true,
            Days: [
                new MealPlanDayModel(
                    Id: Guid.NewGuid(),
                    DayNumber: 1,
                    Meals: [
                        new MealPlanMealModel(
                            Id: mealId,
                            MealType: "Breakfast",
                            RecipeId: recipeId,
                            RecipeName: "Oats",
                            Servings: 2,
                            Calories: 300,
                            Proteins: 20,
                            Fats: 8,
                            Carbs: 40)
                    ])
            ]);

        var response = model.ToHttpResponse();

        Assert.Equal(model.Id, response.Id);
        Assert.Equal("Plan", response.Name);
        Assert.Equal(2100, response.TargetCaloriesPerDay);
        var day = Assert.Single(response.Days);
        Assert.Equal(1, day.DayNumber);
        var meal = Assert.Single(day.Meals);
        Assert.Equal(mealId, meal.Id);
        Assert.Equal(recipeId, meal.RecipeId);
        Assert.Equal("Oats", meal.RecipeName);
        Assert.Equal(300, meal.Calories);
        Assert.Equal(40, meal.Carbs);
    }
}
