using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealItemDisplayReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task BatchDisplay_PreservesSnapshotsLegacyFallbackAndTenantIsolationWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("display-owner@example.com", "hash");
        var other = User.Create("display-other@example.com", "hash");
        var product = Product.Create(user.Id, "Current apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, productType: ProductType.Fruit);
        var meal = Meal.Create(user.Id, DateTime.UtcNow);
        var foreignMeal = Meal.Create(other.Id, DateTime.UtcNow);
        MealItem snapshot = meal.AddProduct(product.Id, 100);
        snapshot.ApplyProductSnapshot("Original apple", imageUrl: null, MeasurementUnit.G, 100, 40, 0.2, 0.1, 10, 2, 0);
        MealItem legacy = meal.AddProduct(product.Id, 150);
        foreignMeal.AddProduct(product.Id, 200);
        context.AddRange(user, other, product, meal, foreignMeal);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new MealItemDisplayReadService(context);
        IReadOnlyList<MealItemDisplayReadModel> items = await service.GetByMealIdsAsync(user.Id, [meal.Id, foreignMeal.Id], CancellationToken.None);

        Assert.Equal(2, items.Count);
        MealItemDisplayReadModel frozen = Assert.Single(items, item => item.Id == snapshot.Id.Value);
        MealItemDisplayReadModel current = Assert.Single(items, item => item.Id == legacy.Id.Value);
        Assert.Equal("Original apple", frozen.ProductName);
        Assert.Equal(40, frozen.ProductCaloriesPerBase);
        Assert.Equal("Current apple", current.ProductName);
        Assert.Equal(52, current.ProductCaloriesPerBase);
        Assert.NotNull(frozen.ProductQualityScore);
        Assert.All(items, item => Assert.Equal(meal.Id.Value, item.MealId));
        Assert.Empty(await service.GetByMealIdsAsync(user.Id, [], CancellationToken.None));
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
