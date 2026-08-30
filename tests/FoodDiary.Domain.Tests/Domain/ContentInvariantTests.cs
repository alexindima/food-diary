using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class ContentInvariantTests {
    [Fact]
    public void FavoriteMeal_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FavoriteMeal.Create(UserId.Empty, MealId.New()));
    }

    [Fact]
    public void FavoriteMeal_Create_WithEmptyMealId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FavoriteMeal.Create(UserId.New(), MealId.Empty));
    }

    [Fact]
    public void FavoriteMeal_Create_WithName_TrimsName() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "  My Breakfast  ");

        Assert.Equal("My Breakfast", fav.Name);
    }

    [Fact]
    public void FavoriteMeal_Create_WithWhitespaceName_SetsNull() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "   ");

        Assert.Null(fav.Name);
    }

    [Fact]
    public void FavoriteMeal_UpdateName_WithNewValue_SetsModifiedOnUtc() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "Old");

        fav.UpdateName("New");

        Assert.Equal("New", fav.Name);
        Assert.NotNull(fav.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteMeal_UpdateName_WithSameValue_DoesNotSetModifiedOnUtc() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "Same");

        fav.UpdateName("Same");

        Assert.Null(fav.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteMeal_UpdateName_WithNull_ClearsName() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "Name");

        fav.UpdateName(name: null);

        Assert.Null(fav.Name);
        Assert.NotNull(fav.ModifiedOnUtc);
    }
}
