using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteMealRestoreTests {
    [Fact]
    public void RemoveAndRestorePreserveIdentityNameAndSortDate() {
        var favorite = FavoriteMeal.Create(UserId.New(), MealId.New(), "Lunch");
        (FavoriteMealId, UserId, MealId, DateTime, DateTime, string?) identity = (favorite.Id, favorite.UserId, favorite.MealId, favorite.CreatedAtUtc, favorite.CreatedOnUtc, favorite.Name);
        favorite.Remove();
        Assert.NotNull(favorite.RemovedAtUtc);
        DateTime? removedAt = favorite.RemovedAtUtc;
        favorite.Remove();
        Assert.Equal(removedAt, favorite.RemovedAtUtc);
        favorite.Restore();
        favorite.Restore();
        Assert.Null(favorite.RemovedAtUtc);
        Assert.Equal(identity, (favorite.Id, favorite.UserId, favorite.MealId, favorite.CreatedAtUtc, favorite.CreatedOnUtc, favorite.Name));
    }
}
