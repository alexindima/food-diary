using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class PreferredProductQuantityTests {
    [Fact]
    public void PreferredQuantity_RetainsUnboundedValueAndCanBeCleared() {
        var favorite = FavoriteProduct.CreateWithPreferredQuantity(UserId.New(), ProductId.New(), quantity: PreferredProductQuantity.FromAmount(2_000_000));
        Assert.Equal(2_000_000, favorite.PreferredPortionAmount);
        favorite.UpdatePreferredQuantity(quantity: null);
        Assert.Null(favorite.PreferredPortionAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void PreferredQuantity_RejectsInvalidValues(double value) => Assert.Throws<ArgumentOutOfRangeException>(() => PreferredProductQuantity.FromAmount(value));

}
