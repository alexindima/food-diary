using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class FavoriteIdConversionTests {
    [Fact]
    public void FavoriteIds_PreserveGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var meal = (FavoriteMealId)value;
        var product = (FavoriteProductId)value;
        var recipe = (FavoriteRecipeId)value;

        Assert.Multiple(
            () => Assert.Equal(value, (Guid)meal),
            () => Assert.Equal(value.ToString(), meal.ToString()),
            () => Assert.Equal(Guid.Empty, FavoriteMealId.Empty.Value),
            () => Assert.Equal(value, (Guid)product),
            () => Assert.Equal(value.ToString(), product.ToString()),
            () => Assert.Equal(value, (Guid)recipe),
            () => Assert.Equal(value.ToString(), recipe.ToString()));
    }
}
