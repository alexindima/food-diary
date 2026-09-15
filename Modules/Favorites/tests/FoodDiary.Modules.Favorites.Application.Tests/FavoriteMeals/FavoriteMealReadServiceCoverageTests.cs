using FoodDiary.Mediator;
using FoodDiary.Testing;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadFavoriteMeals;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoritesOverview;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteIds;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteStatus;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteMeals;

[ExcludeFromCodeCoverage]
public sealed class FavoriteMealReadServiceCoverageTests {
    [Fact]
    public async Task GetOverviewAsync_AppliesLimitAndReturnsUnpagedTotal() {
        var userId = UserId.New();
        IReadOnlyList<FavoriteMealReadModel> favorites = [
            CreateReadModel("First", hour: 8),
        ];
        IFavoriteMealReadModelRepository repository = Substitute.For<IFavoriteMealReadModelRepository>();
        repository.GetOverviewReadModelsAsync(userId, 1, Arg.Any<CancellationToken>()).Returns((favorites, 1_001));
        ISender service = RequestTestSender.Create(new ReadFavoriteMealsQueryHandler(repository), new ReadMealFavoriteStatusQueryHandler(repository), new ReadMealFavoriteIdsQueryHandler(repository), new ReadMealFavoritesOverviewQueryHandler(repository));

        (IReadOnlyList<MealFavoriteMealModel> items, int totalItems) =
            await service.Send(new ReadMealFavoritesOverviewQuery(userId, 1), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(1_001, totalItems),
            () => Assert.Equal("First", Assert.Single(items).Name));
    }

    private static FavoriteMealReadModel CreateReadModel(string name, int hour) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            new DateTime(2026, 7, 13, hour, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 13, hour, 0, 0, DateTimeKind.Utc),
            "Breakfast",
            100,
            10,
            5,
            12,
            1);
}
