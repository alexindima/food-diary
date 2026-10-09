using FoodDiary.Modules.RecentItems.Domain.Entities.Recents;
using FoodDiary.Modules.RecentItems.Domain.Enums;
using FoodDiary.Modules.RecentItems.Domain.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecentItemReferenceTests {
    [Fact]
    public void References_PreserveKindIdentityAndTimestamp() {
        var id = ProductId.New();
        var reference = RecentItemReference.ForProduct(id);
        var now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        var item = RecentItem.CreateWithReference(UserId.New(), reference, now);
        Assert.Multiple(() => Assert.Equal(RecentItemType.Product, item.ItemType), () => Assert.Equal(id.Value, item.ItemId), () => Assert.Equal(now, item.LastUsedAtUtc));
        Assert.Equal(RecentItemType.Recipe, RecentItemReference.ForRecipe(RecipeId.New()).Kind);
        Assert.Throws<ArgumentException>(() => RecentItem.CreateWithReference(UserId.New(), RecentItemReference.ForProduct(ProductId.Empty), now));
    }

}
