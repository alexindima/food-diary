using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoritePortionIdempotencyTests {
    [Fact]
    public void MiscDomainMethods_CoverRemainingBranches() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), "Apple", 100);
        favorite.UpdatePreferredPortionAmount(125);
        favorite.UpdatePreferredPortionAmount(125);
        Assert.Multiple(
            () => Assert.Equal(125, favorite.PreferredPortionAmount));
    }
}
