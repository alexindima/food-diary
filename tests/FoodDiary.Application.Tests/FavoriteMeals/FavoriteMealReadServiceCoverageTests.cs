using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.FavoriteMeals;

[ExcludeFromCodeCoverage]
public sealed class FavoriteMealReadServiceCoverageTests {
    [Fact]
    public async Task GetOverviewAsync_AppliesLimitAndReturnsUnpagedTotal() {
        var userId = UserId.New();
        IReadOnlyList<FavoriteMealReadModel> favorites = [
            CreateReadModel("First", hour: 8),
            CreateReadModel("Second", hour: 9),
        ];
        IFavoriteMealReadModelRepository repository = Substitute.For<IFavoriteMealReadModelRepository>();
        repository.GetAllReadModelsAsync(userId, Arg.Any<CancellationToken>()).Returns(favorites);
        var service = new FavoriteMealReadService(repository);

        (IReadOnlyList<FavoriteMealModel> items, int totalItems) =
            await service.GetOverviewAsync(userId, limit: 1, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(2, totalItems),
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
