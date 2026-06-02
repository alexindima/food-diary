using FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.MealPlans;

public class MealPlansFeatureTests {
    [Fact]
    public async Task AdoptMealPlan_WhenPlanNotFound_ReturnsFailure() {
        var repo = new StubMealPlanRepository(null);
        var handler = new AdoptMealPlanCommandHandler(repo);

        var result = await handler.Handle(
            new AdoptMealPlanCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdoptMealPlan_WhenNotCurated_ReturnsFailure() {
        var userId = UserId.New();
        var plan = MealPlan.CreateForUser(userId, "My Plan", null, DietType.Balanced, 7, null);
        var repo = new StubMealPlanRepository(plan);
        var handler = new AdoptMealPlanCommandHandler(repo);

        var result = await handler.Handle(
            new AdoptMealPlanCommand(Guid.NewGuid(), plan.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotCurated", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdoptMealPlan_WithNullUserId_ReturnsFailure() {
        var handler = new AdoptMealPlanCommandHandler(new StubMealPlanRepository(null));

        var result = await handler.Handle(
            new AdoptMealPlanCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GenerateShoppingList_WithCuratedPlan_AggregatesRecipeIngredients() {
        var userId = UserId.New();
        var product = Product.Create(
            userId,
            "Chicken breast",
            MeasurementUnit.G,
            100,
            null,
            caloriesPerBase: 165,
            proteinsPerBase: 31,
            fatsPerBase: 3.6,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            category: "Meat");
        var recipe = Recipe.Create(userId, "Chicken bowl", servings: 2);
        var step = recipe.AddStep(1, "Cook chicken.");
        var ingredient = step.AddProductIngredient(product.Id, 100);
        SetProperty(ingredient, nameof(ingredient.Product), product);

        var plan = MealPlan.CreateCurated("High protein", null, DietType.Balanced, 1, null);
        var meal = plan.AddDay(1).AddMeal(MealType.Lunch, recipe.Id, servings: 3);
        SetProperty(meal, nameof(meal.Recipe), recipe);

        var shoppingLists = new RecordingShoppingListRepository();
        var handler = new GenerateShoppingListCommandHandler(new StubMealPlanRepository(plan), shoppingLists);

        var result = await handler.Handle(new GenerateShoppingListCommand(userId.Value, plan.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(shoppingLists.Added);
        Assert.Equal("High protein", result.Value.Name);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(product.Id.Value, item.ProductId);
        Assert.Equal("Chicken breast", item.Name);
        Assert.Equal(150, item.Amount);
        Assert.Equal(nameof(MeasurementUnit.G), item.Unit);
        Assert.Equal("Meat", item.Category);
    }

    [Fact]
    public async Task GenerateShoppingList_WhenUserDoesNotOwnPrivatePlan_ReturnsNotFound() {
        var ownerId = UserId.New();
        var plan = MealPlan.CreateForUser(ownerId, "Private plan", null, DietType.Balanced, 1, null);
        var shoppingLists = new RecordingShoppingListRepository();
        var handler = new GenerateShoppingListCommandHandler(new StubMealPlanRepository(plan), shoppingLists);

        var result = await handler.Handle(new GenerateShoppingListCommand(Guid.NewGuid(), plan.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
        Assert.Null(shoppingLists.Added);
    }

    private sealed class StubMealPlanRepository(MealPlan? plan) : IMealPlanRepository {
        public Task<MealPlan?> GetByIdAsync(MealPlanId id, bool includeDays = false, CancellationToken ct = default) =>
            Task.FromResult(plan);

        public Task<MealPlan> AddAsync(MealPlan p, CancellationToken ct = default) => Task.FromResult(p);
        public Task<IReadOnlyList<MealPlan>> GetCuratedAsync(DietType? dietType = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<MealPlan>> GetByUserAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class RecordingShoppingListRepository : IShoppingListRepository {
        public ShoppingList? Added { get; private set; }

        public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) {
            Added = list;
            return Task.FromResult(list);
        }

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static void SetProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) where TTarget : class {
        typeof(TTarget).GetProperty(propertyName)!.SetValue(target, value);
    }
}
