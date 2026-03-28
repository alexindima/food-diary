using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Meals;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class MealRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesDateFilterAndKeepsPagingMetadata() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var olderMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        var newerMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc));
        var filteredOutMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(olderMeal, newerMeal, filteredOutMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 1,
            dateFrom: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));

        var item = Assert.Single(items);
        Assert.Equal(2, totalItems);
        Assert.Equal(newerMeal.Id, item.Id);
    }
}
