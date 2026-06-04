using FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Application.MealPlans.Queries.GetMealPlans;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.MealPlans;

[ExcludeFromCodeCoverage]
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

    [Fact]
    public async Task GetMealPlanById_WhenUserDoesNotOwnPrivatePlan_ReturnsNotFound() {
        var ownerId = UserId.New();
        var plan = MealPlan.CreateForUser(ownerId, "Private plan", null, DietType.Balanced, 1, null);
        var handler = new GetMealPlanByIdQueryHandler(new StubMealPlanRepository(plan));

        var result = await handler.Handle(
            new GetMealPlanByIdQuery(Guid.NewGuid(), plan.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMealPlans_WithDietTypeFilter_ReturnsCuratedAndUserPlans() {
        var userId = UserId.New();
        var curated = MealPlan.CreateCurated("Keto curated", null, DietType.Keto, 1, null);
        var userPlan = MealPlan.CreateForUser(userId, "User plan", null, DietType.Balanced, 1, null);
        var repository = new StubMealPlanRepository(curated, curatedPlans: [curated], userPlans: [userPlan]);
        var handler = new GetMealPlansQueryHandler(repository);

        var result = await handler.Handle(
            new GetMealPlansQuery(userId.Value, "keto"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DietType.Keto, repository.LastDietTypeFilter);
        Assert.Equal(["Keto curated", "User plan"], result.Value.Select(plan => plan.Name));
    }

    [Fact]
    public void MealPlan_ToModel_OrdersDaysAndMealsAndScalesRecipeNutrition() {
        var userId = UserId.New();
        var breakfast = Recipe.Create(userId, "Breakfast bowl", servings: 2);
        breakfast.SetManualNutrition(200, 20, 8, 18, 4, 0);
        var dinner = Recipe.Create(userId, "Dinner bowl", servings: 4);
        dinner.SetManualNutrition(800, 40, 20, 100, 10, 0);
        var plan = MealPlan.CreateCurated("Balanced week", "Plan description", DietType.Balanced, 2, 2200);
        var day2 = plan.AddDay(2);
        var day1 = plan.AddDay(1);
        var dinnerMeal = day1.AddMeal(MealType.Dinner, dinner.Id, servings: 2);
        var breakfastMeal = day1.AddMeal(MealType.Breakfast, breakfast.Id, servings: 1);
        SetProperty(dinnerMeal, nameof(dinnerMeal.Recipe), dinner);
        SetProperty(breakfastMeal, nameof(breakfastMeal.Recipe), breakfast);

        var model = plan.ToModel();

        Assert.Equal(plan.Id.Value, model.Id);
        Assert.Equal("Balanced week", model.Name);
        Assert.Equal("Plan description", model.Description);
        Assert.Equal(nameof(DietType.Balanced), model.DietType);
        Assert.Equal(2, model.DurationDays);
        Assert.Equal(2200, model.TargetCaloriesPerDay);
        Assert.True(model.IsCurated);
        Assert.Equal([1, 2], model.Days.Select(day => day.DayNumber));
        var firstDayMeals = model.Days[0].Meals;
        Assert.Equal([nameof(MealType.Breakfast), nameof(MealType.Dinner)], firstDayMeals.Select(meal => meal.MealType));
        Assert.Equal(100, firstDayMeals[0].Calories);
        Assert.Equal(10, firstDayMeals[0].Proteins);
        Assert.Equal(400, firstDayMeals[1].Calories);
        Assert.Equal(20, firstDayMeals[1].Proteins);
        Assert.Empty(model.Days.Single(day => day.DayNumber == 2).Meals);
    }

    [Fact]
    public void MealPlan_ToSummaryModel_CountsDistinctRecipesAcrossDays() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var otherRecipeId = RecipeId.New();
        var plan = MealPlan.CreateForUser(userId, "User plan", null, DietType.LowCarb, 2, null);
        plan.AddDay(1).AddMeal(MealType.Breakfast, recipeId, servings: 1);
        plan.AddDay(2).AddMeal(MealType.Dinner, recipeId, servings: 1);
        plan.Days.Single(day => day.DayNumber == 2).AddMeal(MealType.Lunch, otherRecipeId, servings: 1);

        var model = plan.ToSummaryModel();

        Assert.Equal(plan.Id.Value, model.Id);
        Assert.Equal("User plan", model.Name);
        Assert.Equal(nameof(DietType.LowCarb), model.DietType);
        Assert.False(model.IsCurated);
        Assert.Equal(2, model.TotalRecipes);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubMealPlanRepository(
        MealPlan? plan,
        IReadOnlyList<MealPlan>? curatedPlans = null,
        IReadOnlyList<MealPlan>? userPlans = null) : IMealPlanRepository {
        public DietType? LastDietTypeFilter { get; private set; }

        public Task<MealPlan?> GetByIdAsync(MealPlanId id, bool includeDays = false, CancellationToken ct = default) =>
            Task.FromResult(plan);

        public Task<MealPlan> AddAsync(MealPlan p, CancellationToken ct = default) => Task.FromResult(p);

        public Task<IReadOnlyList<MealPlan>> GetCuratedAsync(DietType? dietType = null, CancellationToken ct = default) {
            LastDietTypeFilter = dietType;
            return Task.FromResult(curatedPlans ?? []);
        }

        public Task<IReadOnlyList<MealPlan>> GetByUserAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(userPlans ?? []);
    }

    [ExcludeFromCodeCoverage]
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
