using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Modules.Usda.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class SharedReferenceDataContextIntegrationTests {
    [Fact]
    public void UsdaModelContainsOnlyFiveOwnedReferenceTables() {
        using var context = new UsdaDbContext(new DbContextOptionsBuilder<UsdaDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Type[] expected = [typeof(UsdaFood), typeof(UsdaNutrient), typeof(UsdaFoodNutrient),
            typeof(UsdaFoodPortion), typeof(DailyReferenceValue)];
        Assert.Equal(expected.OrderBy(type => type.Name, StringComparer.Ordinal),
            context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        string[] tables = ["DailyReferenceValues", "UsdaFoodNutrients", "UsdaFoodPortions", "UsdaFoods", "UsdaNutrients"];
        Assert.Equal(tables, context.Model.GetEntityTypes().Select(entity => entity.GetTableName()!).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }
}
