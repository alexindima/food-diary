using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.MealPlans;

public partial class MealPlansFeatureTests {
    [Fact]
    public async Task PublishedUnsupportedUnits_RemainSeparateAndRetainTheirQuantityAsNotes() {
        var viewer = UserId.New();
        var recipeId = RecipeId.New();
        var plan = MealPlan.CreateCurated("Public recipe", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Lunch, recipeId, 1).SetRecipeSnapshot(new MealPlanRecipeSnapshot(recipeId, "Recipe", 1, [
            new MealPlanRecipeIngredientSnapshot(ProductId: null, 1, "Rice", BaseUnit: null, Category: null, PublishedUnit: "cup"),
            new MealPlanRecipeIngredientSnapshot(ProductId: null, 2, "Rice", BaseUnit: null, Category: null, PublishedUnit: "tbsp"),
        ]));
        var handler = new GenerateShoppingListCommandHandler(new StubMealPlanRepository(plan), new RecordingShoppingListRepository(), CreateCurrentUserAccessService());
        Result<ShoppingListModel> result = await handler.Handle(new GenerateShoppingListCommand(viewer.Value, plan.Id.Value), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, item => { Assert.Null(item.ProductId); Assert.Null(item.Amount); Assert.Null(item.Unit); });
        Assert.Contains(result.Value.Items, item => string.Equals(item.Note, "1 cup", StringComparison.Ordinal));
        Assert.Contains(result.Value.Items, item => string.Equals(item.Note, "2 tbsp", StringComparison.Ordinal));
    }
}
