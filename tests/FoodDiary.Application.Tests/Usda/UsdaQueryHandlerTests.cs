using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Usda.Queries.GetMicronutrients;
using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;
using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Tests.Usda;

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

    private sealed class StubUsdaFoodRepository : IUsdaFoodRepository {
        public IReadOnlyList<UsdaFood> SearchResults { get; init; } = [];
        public UsdaFood? Food { get; init; }
        public IReadOnlyList<UsdaFoodNutrient> Nutrients { get; init; } = [];
        public IReadOnlyList<UsdaFoodPortion> Portions { get; init; } = [];
        public IReadOnlyDictionary<int, DailyReferenceValue> DailyValues { get; init; } = new Dictionary<int, DailyReferenceValue>();

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
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
            string ageGroup = "adult",
            string gender = "all",
            CancellationToken cancellationToken = default) =>
            Task.FromResult(DailyValues);
    }

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
}
