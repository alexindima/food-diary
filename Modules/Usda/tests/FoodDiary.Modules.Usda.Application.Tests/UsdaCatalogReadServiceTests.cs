using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Application.CatalogImport;
using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsdaCatalogReadServiceTests {
    [Fact]
    public async Task ReadLocalFoodsAsync_UsesDistinctKnownFoodsAndAttachesOnlyTheirNutrients() {
        int[] requested = [123, 456, 123, 789];
        var energy = new UsdaNutrientReadModel(1008, "Energy", "kcal", 343);
        IUsdaFoodReadModelRepository repository = Substitute.For<IUsdaFoodReadModelRepository>();
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        repository.GetNutrientReadModelsByFdcIdsAsync(requested, token)
            .Returns(new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>> { [123] = [energy], [789] = [energy] });
        repository.GetByFdcIdReadModelAsync(123, token).Returns(new UsdaFoodReadModel(123, "Buckwheat", FoodCategory: null));
        repository.GetByFdcIdReadModelAsync(456, token).Returns(new UsdaFoodReadModel(456, "No nutrient data", FoodCategory: null));

        IReadOnlyDictionary<int, UsdaCatalogFoodModel> foods = await new UsdaCatalogReadService(repository).ReadLocalFoodsAsync(requested, token);

        Assert.Equal([123, 456], foods.Keys.Order());
        UsdaNutrientReadModel nutrient = Assert.Single(foods[123].Nutrients);
        Assert.Multiple(
            () => Assert.Equal("Buckwheat", foods[123].Description),
            () => Assert.Equal(energy, nutrient),
            () => Assert.Empty(foods[456].Nutrients));
        await repository.Received(1).GetNutrientReadModelsByFdcIdsAsync(requested, token);
        await repository.Received(1).GetByFdcIdReadModelAsync(123, token);
    }
}
