using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.MealPlans;

public partial class MealPlansFeatureTests {
    [Theory]
    [InlineData("100 g")]
    [InlineData("to taste")]
    [InlineData(null)]
    public async Task TextIngredients_PreserveEachLiteralUseWithoutChangingMeasuredIngredientTotals(string? amountText) {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var productId = ProductId.New();
        var snapshot = new MealPlanRecipeSnapshot(recipeId, "Rice recipe", 2, [
            new MealPlanRecipeIngredientSnapshot(productId, 100, "Rice", MeasurementUnit.G, Category: null),
            new MealPlanRecipeIngredientSnapshot(ProductId: null, 0, "Rice", BaseUnit: null, Category: null) {
                IsText = true,
                AmountText = amountText,
            },
        ]);
        var plan = MealPlan.CreateForUser(userId, "Literal plan", description: null, DietType.Balanced, 2, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Lunch, recipeId, 2).SetRecipeSnapshot(snapshot);
        plan.AddDay(2).AddMeal(MealType.Dinner, recipeId, 1).SetRecipeSnapshot(snapshot);
        var handler = new GenerateShoppingListCommandHandler(new StubMealPlanRepository(plan),
            new RecordingShoppingListRepository(), CreateCurrentUserAccessService());

        Result<ShoppingListModel> result = await handler.Handle(new GenerateShoppingListCommand(userId.Value, plan.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(3, result.Value.Items.Count);
        ShoppingListItemModel measured = Assert.Single(result.Value.Items, item => item.ProductId == productId.Value);
        Assert.Multiple(
            () => Assert.Equal(150, measured.Amount),
            () => Assert.Equal(nameof(MeasurementUnit.G), measured.Unit),
            () => Assert.Equal(2, measured.Sources.Count));
        ShoppingListItemModel[] textItems = [.. result.Value.Items.Where(item => item.ProductId is null)];
        Assert.Equal(2, textItems.Length);
        Assert.All(textItems, item => Assert.Multiple(
            () => Assert.Equal("Rice", item.Name),
            () => Assert.Null(item.Amount),
            () => Assert.Null(item.Unit),
            () => Assert.Equal(amountText, item.Note),
            () => Assert.Empty(item.Sources)));
    }
}
