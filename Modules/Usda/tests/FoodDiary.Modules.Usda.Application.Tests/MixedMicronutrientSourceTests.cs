using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Usda.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MixedMicronutrientSourceTests {
    [Fact]
    public async Task MixedLocalAndProviderFoods_PreserveLocalNutrientsAndScaleBothSourcesFromOneHundredGrams() {
        var user = UserId.New();
        var date = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);
        using var cancellation = new CancellationTokenSource();
        IUsdaMealNutritionReadService meals = Substitute.For<IUsdaMealNutritionReadService>();
        meals.GetForDateAsync(user, date, 1001, cancellation.Token).Returns((IReadOnlyList<UsdaMealProductNutritionReadModel>)[
            new(150, MeasurementUnit.G, 10), new(75, MeasurementUnit.G, 20),
        ]);
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> local = new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>> {
            [10] = [new(1162, "Vitamin C", "mg", 20)],
        };
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> references = new Dictionary<int, UsdaDailyReferenceValueReadModel> {
            [1162] = new(1162, 80, "mg"),
        };
        IUsdaFoodReadModelRepository repository = Substitute.For<IUsdaFoodReadModelRepository>();
        repository.GetNutrientReadModelsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), cancellation.Token).Returns(local);
        repository.GetDailyReferenceValueReadModelsAsync(cancellationToken: cancellation.Token).Returns(references);
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        provider.GetFoodDetailAsync(20, Arg.Any<CancellationToken>()).Returns(new UsdaFoodDetailModel(
            20, "Provider food", FoodCategory: null, [new(1162, "Vitamin C", "mg", 40, DailyValue: null, PercentDailyValue: null)], [], HealthScores: null));
        var handler = new GetDailyMicronutrientsQueryHandler(meals, repository, Substitute.For<ICurrentUserAccessService>(), provider, TimeProvider.System);

        DailyMicronutrientSummaryModel result = ResultAssert.Success(await handler.Handle(new GetDailyMicronutrientsQuery(user.Value, date), cancellation.Token));

        DailyMicronutrientModel nutrient = Assert.Single(result.Nutrients);
        Assert.Multiple(
            () => Assert.Equal(date, result.Date),
            () => Assert.Equal(2, result.LinkedProductCount),
            () => Assert.Equal(2, result.TotalProductCount),
            () => Assert.Equal(1162, nutrient.NutrientId),
            () => Assert.Equal("mg", nutrient.Unit),
            () => Assert.Equal(60, nutrient.TotalAmount),
            () => Assert.Equal(80, nutrient.DailyValue),
            () => Assert.Equal(75, nutrient.PercentDailyValue),
            () => Assert.Equal(20, local[10][0].Amount));
        await provider.Received(1).GetFoodDetailAsync(20, Arg.Any<CancellationToken>());
        Assert.Single(provider.ReceivedCalls());
        await meals.Received(1).GetForDateAsync(user, date, 1001, cancellation.Token);
    }
}
