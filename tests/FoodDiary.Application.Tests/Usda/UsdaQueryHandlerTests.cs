using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;
using FoodDiary.Application.Usda.Queries.GetMicronutrients;
using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Usda;

[ExcludeFromCodeCoverage]
public sealed class UsdaQueryHandlerTests {
    [Fact]
    public async Task SearchUsdaFoods_WhenLocalResultsAreSparse_AddsNonDuplicateBrandedResults() {
        var repository = new StubUsdaFoodRepository {
            SearchResults = [
                new UsdaFood { FdcId = 1, Description = "Chicken breast", FoodCategory = "Poultry" }
            ]
        };
        var branded = new StubUsdaFoodSearchService {
            SearchResults = [
                new UsdaFoodModel(1, "Duplicate chicken", "Branded"),
                new UsdaFoodModel(2, "Branded chicken", "Branded")
            ]
        };
        var handler = new SearchUsdaFoodsQueryHandler(repository, branded);

        var result = await handler.Handle(new SearchUsdaFoodsQuery("chicken", Limit: 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2], result.Value.Select(f => f.FdcId));
        Assert.Equal(2, branded.LastLimit);
    }

    [Fact]
    public async Task SearchUsdaFoods_WhenLocalResultsFillLimit_DoesNotCallBrandedSearch() {
        var repository = new StubUsdaFoodRepository {
            SearchResults = [
                new UsdaFood { FdcId = 1, Description = "Chicken breast" },
                new UsdaFood { FdcId = 2, Description = "Chicken thigh" }
            ]
        };
        var branded = new StubUsdaFoodSearchService();
        var handler = new SearchUsdaFoodsQueryHandler(repository, branded);

        var result = await handler.Handle(new SearchUsdaFoodsQuery("chicken", Limit: 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(branded.SearchCalled);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetMicronutrients_WithLocalFood_MapsNutrientsPortionsAndDailyValues() {
        var repository = new StubUsdaFoodRepository {
            Food = new UsdaFood { FdcId = 10, Description = "Spinach", FoodCategory = "Vegetables" },
            Nutrients = [
                CreateNutrient(10, nutrientId: 301, "Calcium", "mg", amount: 120)
            ],
            Portions = [
                new UsdaFoodPortion {
                    Id = 7,
                    FdcId = 10,
                    Amount = 1,
                    MeasureUnitName = "cup",
                    GramWeight = 30,
                    PortionDescription = "1 cup",
                    Modifier = "chopped"
                }
            ],
            DailyValues = new Dictionary<int, DailyReferenceValue> {
                [301] = new() { NutrientId = 301, Value = 1000, Unit = "mg", AgeGroup = "adult", Gender = "all" }
            }
        };
        var handler = new GetMicronutrientsQueryHandler(repository, new StubUsdaFoodSearchService());

        var result = await handler.Handle(new GetMicronutrientsQuery(10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Spinach", result.Value.Description);
        var calcium = Assert.Single(result.Value.Nutrients);
        Assert.Equal(120, calcium.AmountPer100g);
        Assert.Equal(1000, calcium.DailyValue);
        Assert.Equal(12, calcium.PercentDailyValue);
        var portion = Assert.Single(result.Value.Portions);
        Assert.Equal("cup", portion.MeasureUnitName);
    }

    [Fact]
    public async Task GetMicronutrients_WhenLocalFoodMissing_UsesBrandedDetailAndDailyValues() {
        var repository = new StubUsdaFoodRepository {
            DailyValues = new Dictionary<int, DailyReferenceValue> {
                [203] = new() { NutrientId = 203, Value = 50, Unit = "g", AgeGroup = "adult", Gender = "all" }
            }
        };
        var branded = new StubUsdaFoodSearchService {
            Detail = new UsdaFoodDetailModel(
                20,
                "Branded yogurt",
                "Branded",
                [new MicronutrientModel(203, "Protein", "g", 10, null, null)],
                [],
                null)
        };
        var handler = new GetMicronutrientsQueryHandler(repository, branded);

        var result = await handler.Handle(new GetMicronutrientsQuery(20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Branded yogurt", result.Value.Description);
        Assert.Equal(20, result.Value.Nutrients.Single().PercentDailyValue);
    }

    [Fact]
    public async Task GetMicronutrients_WhenFoodMissingEverywhere_ReturnsNotFound() {
        var handler = new GetMicronutrientsQueryHandler(
            new StubUsdaFoodRepository(),
            new StubUsdaFoodSearchService());

        var result = await handler.Handle(new GetMicronutrientsQuery(999), CancellationToken.None);

        Assert.True(result.IsFailure);
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
            null,
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
            null,
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
        var meals = new StubMealRepository { Meals = [meal] };
        var repository = new StubUsdaFoodRepository {
            NutrientsByFdcId = new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>> {
                [10] = [
                    CreateNutrient(10, nutrientId: 301, "Calcium", "mg", amount: 120),
                    CreateNutrient(10, nutrientId: 303, "Iron", "mg", amount: 2.5)
                ]
            },
            DailyValues = new Dictionary<int, DailyReferenceValue> {
                [301] = new() { NutrientId = 301, Value = 1000, Unit = "mg", AgeGroup = "adult", Gender = "all" },
                [303] = new() { NutrientId = 303, Value = 18, Unit = "mg", AgeGroup = "adult", Gender = "all" }
            }
        };
        var handler = new GetDailyMicronutrientsQueryHandler(meals, repository);

        var result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.LinkedProductCount);
        Assert.Equal(3, result.Value.TotalProductCount);
        Assert.NotNull(result.Value.HealthScores);
        var calcium = Assert.Single(result.Value.Nutrients, nutrient => nutrient.NutrientId == 301);
        Assert.Equal(240, calcium.TotalAmount);
        Assert.Equal(1000, calcium.DailyValue);
        Assert.Equal(24, calcium.PercentDailyValue);
        var iron = Assert.Single(result.Value.Nutrients, nutrient => nutrient.NutrientId == 303);
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
            null,
            caloriesPerBase: 130,
            proteinsPerBase: 2.7,
            fatsPerBase: 0.3,
            carbsPerBase: 28,
            fiberPerBase: 0.4,
            alcoholPerBase: 0);
        var meal = Meal.Create(userId, date);
        AddProductItem(meal, product, 100);
        var repository = new StubUsdaFoodRepository();
        var handler = new GetDailyMicronutrientsQueryHandler(new StubMealRepository { Meals = [meal] }, repository);

        var result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.LinkedProductCount);
        Assert.Equal(1, result.Value.TotalProductCount);
        Assert.Empty(result.Value.Nutrients);
        Assert.Null(result.Value.HealthScores);
        Assert.False(repository.NutrientsByFdcIdsCalled);
    }

    [Fact]
    public async Task GetDailyMicronutrients_WithNullUserId_ReturnsFailure() {
        var handler = new GetDailyMicronutrientsQueryHandler(new StubMealRepository(), new StubUsdaFoodRepository());

        var result = await handler.Handle(
            new GetDailyMicronutrientsQuery(null, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
            null,
            caloriesPerBase: 23,
            proteinsPerBase: 2.9,
            fatsPerBase: 0.4,
            carbsPerBase: 3.6,
            fiberPerBase: 2.2,
            alcoholPerBase: 0);
        product.LinkToUsdaFood(10);
        var meal = Meal.Create(userId, date);
        AddProductItem(meal, product, 50);
        var repository = new StubUsdaFoodRepository {
            NutrientsByFdcId = new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>(),
            DailyValues = new Dictionary<int, DailyReferenceValue>()
        };
        var handler = new GetDailyMicronutrientsQueryHandler(
            new StubMealRepository { Meals = [meal] },
            repository);

        var result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            Nutrient = new UsdaNutrient { Id = nutrientId, Name = name, UnitName = unit }
        };

    [ExcludeFromCodeCoverage]
    private sealed class StubUsdaFoodRepository : IUsdaFoodRepository {
        public IReadOnlyList<UsdaFood> SearchResults { get; init; } = [];
        public UsdaFood? Food { get; init; }
        public IReadOnlyList<UsdaFoodNutrient> Nutrients { get; init; } = [];
        public IReadOnlyList<UsdaFoodPortion> Portions { get; init; } = [];
        public IReadOnlyDictionary<int, DailyReferenceValue> DailyValues { get; init; } = new Dictionary<int, DailyReferenceValue>();
        public IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> NutrientsByFdcId { get; init; } =
            new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>();
        public bool NutrientsByFdcIdsCalled { get; private set; }

        public Task<IReadOnlyList<UsdaFood>> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UsdaFood>>(SearchResults.Take(limit).ToList());

        public Task<UsdaFood?> GetByFdcIdAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Food?.FdcId == fdcId ? Food : null);

        public Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Nutrients);

        public Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Portions);

        public Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
            IEnumerable<int> fdcIds,
            CancellationToken cancellationToken = default) {
            NutrientsByFdcIdsCalled = true;
            var requested = fdcIds.ToHashSet();
            return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>>(
                NutrientsByFdcId
                    .Where(kvp => requested.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
            string ageGroup = "adult",
            string gender = "all",
            CancellationToken cancellationToken = default) =>
            Task.FromResult(DailyValues);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUsdaFoodSearchService : IUsdaFoodSearchService {
        public IReadOnlyList<UsdaFoodModel> SearchResults { get; init; } = [];
        public UsdaFoodDetailModel? Detail { get; init; }
        public bool SearchCalled { get; private set; }
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
            string query,
            int limit = 20,
            CancellationToken cancellationToken = default) {
            SearchCalled = true;
            LastLimit = limit;
            return Task.FromResult<IReadOnlyList<UsdaFoodModel>>(SearchResults.Take(limit).ToList());
        }

        public Task<UsdaFoodDetailModel?> GetFoodDetailAsync(int fdcId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail?.FdcId == fdcId ? Detail : null);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubMealRepository : IMealRepository {
        public IReadOnlyList<Meal> Meals { get; init; } = [];

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
            UserId userId,
            DateTime date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Meals);
    }

    private static void AddProductItem(Meal meal, Product product, double amount) {
        var item = meal.AddProduct(product.Id, amount);
        typeof(MealItem).GetProperty(nameof(MealItem.Product))!.SetValue(item, product);
    }
}
