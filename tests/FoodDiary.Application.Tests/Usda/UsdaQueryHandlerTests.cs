using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;
using FoodDiary.Application.Usda.Queries.GetMicronutrients;
using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;
using FoodDiary.Application.Usda.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Tests.Usda;

[ExcludeFromCodeCoverage]
public sealed class UsdaQueryHandlerTests {
    [Fact]
    public async Task SearchUsdaFoods_WhenLocalResultsAreSparse_AddsNonDuplicateBrandedResults() {
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(
            searchResults: [
                new UsdaFood { FdcId = 1, Description = "Chicken breast", FoodCategory = "Poultry" },
            ]);
        IUsdaFoodSearchService branded = CreateUsdaFoodSearchService(
            searchResults: [
                new UsdaFoodModel(1, "Duplicate chicken", "Branded"),
                new UsdaFoodModel(2, "Branded chicken", "Branded"),
            ],
            getLastLimit: out Func<int> getLastLimit);
        var handler = new SearchUsdaFoodsQueryHandler(new UsdaFoodReadService(repository, branded));

        Result<IReadOnlyList<UsdaFoodModel>> result = await handler.Handle(new SearchUsdaFoodsQuery("chicken", Limit: 3), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal([1, 2], result.Value.Select(f => f.FdcId));
        Assert.Equal(2, getLastLimit());
    }

    [Fact]
    public async Task SearchUsdaFoods_WhenLocalResultsFillLimit_DoesNotCallBrandedSearch() {
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(
            searchResults: [
                new UsdaFood { FdcId = 1, Description = "Chicken breast" },
                new UsdaFood { FdcId = 2, Description = "Chicken thigh" },
            ]);
        IUsdaFoodSearchService branded = CreateUsdaFoodSearchService(searchCalled: out Func<bool> wasSearchCalled);
        var handler = new SearchUsdaFoodsQueryHandler(new UsdaFoodReadService(repository, branded));

        Result<IReadOnlyList<UsdaFoodModel>> result = await handler.Handle(new SearchUsdaFoodsQuery("chicken", Limit: 2), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(wasSearchCalled());
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetMicronutrients_WithLocalFood_MapsNutrientsPortionsAndDailyValues() {
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(
            food: new UsdaFood { FdcId = 10, Description = "Spinach", FoodCategory = "Vegetables" },
            nutrients: [
                CreateNutrient(10, nutrientId: 301, "Calcium", "mg", amount: 120),
            ],
            portions: [
                new UsdaFoodPortion {
                    Id = 7,
                    FdcId = 10,
                    Amount = 1,
                    MeasureUnitName = "cup",
                    GramWeight = 30,
                    PortionDescription = "1 cup",
                    Modifier = "chopped",
                },
            ],
            dailyValues: new Dictionary<int, DailyReferenceValue> {
                [301] = new() { NutrientId = 301, Value = 1000, Unit = "mg", AgeGroup = "adult", Gender = "all" },
            });
        var handler = new GetMicronutrientsQueryHandler(new UsdaFoodReadService(repository, CreateUsdaFoodSearchService()));

        Result<UsdaFoodDetailModel> result = await handler.Handle(new GetMicronutrientsQuery(10), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Spinach", result.Value.Description);
        MicronutrientModel calcium = Assert.Single(result.Value.Nutrients);
        Assert.Equal(120, calcium.AmountPer100g);
        Assert.Equal(1000, calcium.DailyValue);
        Assert.Equal(12, calcium.PercentDailyValue);
        UsdaFoodPortionModel portion = Assert.Single(result.Value.Portions);
        Assert.Equal("cup", portion.MeasureUnitName);
    }

    [Fact]
    public async Task GetMicronutrients_WhenLocalFoodMissing_UsesBrandedDetailAndDailyValues() {
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(
            dailyValues: new Dictionary<int, DailyReferenceValue> {
                [203] = new() { NutrientId = 203, Value = 50, Unit = "g", AgeGroup = "adult", Gender = "all" },
            });
        IUsdaFoodSearchService branded = CreateUsdaFoodSearchService(
            detail: new UsdaFoodDetailModel(
                20,
                "Branded yogurt",
                "Branded",
                [new MicronutrientModel(203, "Protein", "g", 10, DailyValue: null, PercentDailyValue: null)],
                [],
                HealthScores: null));
        var handler = new GetMicronutrientsQueryHandler(new UsdaFoodReadService(repository, branded));

        Result<UsdaFoodDetailModel> result = await handler.Handle(new GetMicronutrientsQuery(20), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Branded yogurt", result.Value.Description);
        Assert.Equal(20, result.Value.Nutrients.Single().PercentDailyValue);
    }

    [Fact]
    public async Task GetMicronutrients_WhenFoodMissingEverywhere_ReturnsNotFound() {
        var handler = new GetMicronutrientsQueryHandler(new UsdaFoodReadService(
            CreateUsdaFoodRepository(),
            CreateUsdaFoodSearchService()));

        Result<UsdaFoodDetailModel> result = await handler.Handle(new GetMicronutrientsQuery(999), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Usda.FoodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyMicronutrients_WithLinkedProducts_AggregatesScaledNutrientsAndDailyValues() {
        var userId = UserId.New();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var linkedProduct = Product.Create(
            userId,
            "Spinach",
            MeasurementUnit.G,
            100,
            defaultPortionAmount: null,
            caloriesPerBase: 23,
            proteinsPerBase: 2.9,
            fatsPerBase: 0.4,
            carbsPerBase: 3.6,
            fiberPerBase: 2.2,
            alcoholPerBase: 0);
        linkedProduct.LinkToUsdaFood(10);
        var unlinkedProduct = Product.Create(
            userId,
            "Rice",
            MeasurementUnit.G,
            100,
            defaultPortionAmount: null,
            caloriesPerBase: 130,
            proteinsPerBase: 2.7,
            fatsPerBase: 0.3,
            carbsPerBase: 28,
            fiberPerBase: 0.4,
            alcoholPerBase: 0);
        var meal = Meal.Create(userId, date);
        AddProductItem(meal, linkedProduct, 50);
        AddProductItem(meal, linkedProduct, 150);
        AddProductItem(meal, unlinkedProduct, 100);
        IMealRepository meals = CreateMealRepository([meal]);
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(
            dailyValues: new Dictionary<int, DailyReferenceValue> {
                [301] = new() { NutrientId = 301, Value = 1000, Unit = "mg", AgeGroup = "adult", Gender = "all" },
                [303] = new() { NutrientId = 303, Value = 18, Unit = "mg", AgeGroup = "adult", Gender = "all" },
            },
            nutrientsByFdcId: new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>> {
                [10] = [
                    CreateNutrient(10, nutrientId: 301, "Calcium", "mg", amount: 120),
                    CreateNutrient(10, nutrientId: 303, "Iron", "mg", amount: 2.5),
                ],
            });
        var handler = new GetDailyMicronutrientsQueryHandler(new UsdaDailyMicronutrientReadService(meals, repository));

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.LinkedProductCount);
        Assert.Equal(3, result.Value.TotalProductCount);
        Assert.NotNull(result.Value.HealthScores);
        DailyMicronutrientModel calcium = Assert.Single(result.Value.Nutrients, nutrient => nutrient.NutrientId == 301);
        Assert.Equal(240, calcium.TotalAmount);
        Assert.Equal(1000, calcium.DailyValue);
        Assert.Equal(24, calcium.PercentDailyValue);
        DailyMicronutrientModel iron = Assert.Single(result.Value.Nutrients, nutrient => nutrient.NutrientId == 303);
        Assert.Equal(5, iron.TotalAmount);
        Assert.Equal(27.8, iron.PercentDailyValue);
    }

    [Fact]
    public async Task GetDailyMicronutrients_WithNoLinkedProducts_ReturnsEmptySummaryWithoutUsdaLookup() {
        var userId = UserId.New();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var product = Product.Create(
            userId,
            "Rice",
            MeasurementUnit.G,
            100,
            defaultPortionAmount: null,
            caloriesPerBase: 130,
            proteinsPerBase: 2.7,
            fatsPerBase: 0.3,
            carbsPerBase: 28,
            fiberPerBase: 0.4,
            alcoholPerBase: 0);
        var meal = Meal.Create(userId, date);
        AddProductItem(meal, product, 100);
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(nutrientsByFdcIdsCalled: out Func<bool> wereNutrientsByFdcIdsCalled);
        var handler = new GetDailyMicronutrientsQueryHandler(
            new UsdaDailyMicronutrientReadService(CreateMealRepository([meal]), repository));

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.LinkedProductCount);
        Assert.Equal(1, result.Value.TotalProductCount);
        Assert.Empty(result.Value.Nutrients);
        Assert.Null(result.Value.HealthScores);
        Assert.False(wereNutrientsByFdcIdsCalled());
    }

    [Fact]
    public async Task GetDailyMicronutrients_WithNullUserId_ReturnsFailure() {
        var handler = new GetDailyMicronutrientsQueryHandler(
            new UsdaDailyMicronutrientReadService(CreateMealRepository([]), CreateUsdaFoodRepository()));

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(
            new GetDailyMicronutrientsQuery(UserId: null, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetDailyMicronutrients_WhenNutrientsForLinkedProductAreMissing_SkipsProductNutrients() {
        var userId = UserId.New();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var product = Product.Create(
            userId,
            "Spinach",
            MeasurementUnit.G,
            100,
            defaultPortionAmount: null,
            caloriesPerBase: 23,
            proteinsPerBase: 2.9,
            fatsPerBase: 0.4,
            carbsPerBase: 3.6,
            fiberPerBase: 2.2,
            alcoholPerBase: 0);
        product.LinkToUsdaFood(10);
        var meal = Meal.Create(userId, date);
        AddProductItem(meal, product, 50);
        IUsdaFoodRepository repository = CreateUsdaFoodRepository(
            dailyValues: new Dictionary<int, DailyReferenceValue>(),
            nutrientsByFdcId: new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>());
        var handler = new GetDailyMicronutrientsQueryHandler(
            new UsdaDailyMicronutrientReadService(CreateMealRepository([meal]), repository));

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.LinkedProductCount);
        Assert.Equal(1, result.Value.TotalProductCount);
        Assert.Empty(result.Value.Nutrients);
        Assert.NotNull(result.Value.HealthScores);
    }

    private static UsdaFoodNutrient CreateNutrient(
        int fdcId,
        int nutrientId,
        string name,
        string unit,
        double amount) =>
        new() {
            FdcId = fdcId,
            NutrientId = nutrientId,
            Amount = amount,
            Nutrient = new UsdaNutrient { Id = nutrientId, Name = name, UnitName = unit },
        };

    private static IUsdaFoodRepository CreateUsdaFoodRepository(
        IReadOnlyList<UsdaFood>? searchResults = null,
        UsdaFood? food = null,
        IReadOnlyList<UsdaFoodNutrient>? nutrients = null,
        IReadOnlyList<UsdaFoodPortion>? portions = null,
        IReadOnlyDictionary<int, DailyReferenceValue>? dailyValues = null,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>? nutrientsByFdcId = null) =>
        CreateUsdaFoodRepository(
            searchResults,
            food,
            nutrients,
            portions,
            dailyValues,
            nutrientsByFdcId,
            out _);

    private static IUsdaFoodRepository CreateUsdaFoodRepository(
        IReadOnlyList<UsdaFood>? searchResults,
        UsdaFood? food,
        IReadOnlyList<UsdaFoodNutrient>? nutrients,
        IReadOnlyList<UsdaFoodPortion>? portions,
        IReadOnlyDictionary<int, DailyReferenceValue>? dailyValues,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>? nutrientsByFdcId,
        out Func<bool> nutrientsByFdcIdsCalled) {
        bool wasNutrientsByFdcIdsCalled = false;
        nutrientsByFdcIdsCalled = () => wasNutrientsByFdcIdsCalled;

        IUsdaFoodRepository repository = Substitute.For<IUsdaFoodRepository>();
        repository
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int limit = call.ArgAt<int>(1);
                return Task.FromResult<IReadOnlyList<UsdaFood>>((searchResults ?? []).Take(limit).ToList());
            });
        repository
            .GetByFdcIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int fdcId = call.ArgAt<int>(0);
                return Task.FromResult(food?.FdcId == fdcId ? food : null);
            });
        repository
            .GetNutrientsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(nutrients ?? []));
        repository
            .GetPortionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(portions ?? []));
        repository
            .GetNutrientsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                wasNutrientsByFdcIdsCalled = true;
                var requested = call.ArgAt<IEnumerable<int>>(0).ToHashSet();
                IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> values = nutrientsByFdcId ?? new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>();
                return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>>(
                    values
                        .Where(kvp => requested.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            });
        repository
            .GetDailyReferenceValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dailyValues ?? new Dictionary<int, DailyReferenceValue>()));

        return repository;
    }

    private static IUsdaFoodRepository CreateUsdaFoodRepository(out Func<bool> nutrientsByFdcIdsCalled) =>
        CreateUsdaFoodRepository(
            searchResults: null,
            food: null,
            nutrients: null,
            portions: null,
            dailyValues: null,
            nutrientsByFdcId: null,
            nutrientsByFdcIdsCalled: out nutrientsByFdcIdsCalled);

    private static IUsdaFoodSearchService CreateUsdaFoodSearchService(
        IReadOnlyList<UsdaFoodModel>? searchResults = null,
        UsdaFoodDetailModel? detail = null) =>
        CreateUsdaFoodSearchService(searchResults, detail, out _, out _);

    private static IUsdaFoodSearchService CreateUsdaFoodSearchService(
        out Func<bool> searchCalled) =>
        CreateUsdaFoodSearchService(searchResults: null, detail: null, searchCalled: out searchCalled, getLastLimit: out _);

    private static IUsdaFoodSearchService CreateUsdaFoodSearchService(
        IReadOnlyList<UsdaFoodModel>? searchResults,
        out Func<int> getLastLimit) =>
        CreateUsdaFoodSearchService(searchResults, detail: null, searchCalled: out _, getLastLimit: out getLastLimit);

    private static IUsdaFoodSearchService CreateUsdaFoodSearchService(
        IReadOnlyList<UsdaFoodModel>? searchResults,
        UsdaFoodDetailModel? detail,
        out Func<bool> searchCalled,
        out Func<int> getLastLimit) {
        bool wasSearchCalled = false;
        int lastLimit = 0;
        searchCalled = () => wasSearchCalled;
        getLastLimit = () => lastLimit;

        IUsdaFoodSearchService service = Substitute.For<IUsdaFoodSearchService>();
        service
            .SearchBrandedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                wasSearchCalled = true;
                lastLimit = call.ArgAt<int>(1);
                return Task.FromResult<IReadOnlyList<UsdaFoodModel>>((searchResults ?? []).Take(lastLimit).ToList());
            });
        service
            .GetFoodDetailAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int fdcId = call.ArgAt<int>(0);
                return Task.FromResult(detail?.FdcId == fdcId ? detail : null);
            });

        return service;
    }

    private static IMealRepository CreateMealRepository(IReadOnlyList<Meal> meals) {
        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetWithItemsAndProductsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(meals));

        return repository;
    }

    private static void AddProductItem(Meal meal, Product product, double amount) {
        MealItem item = meal.AddProduct(product.Id, amount);
        typeof(MealItem).GetProperty(nameof(MealItem.Product))!.SetValue(item, product);
    }
}
