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

    [Theory]
    [InlineData(RecentItemType.Product, "ce65df28-cbc2-4a2d-98b2-906064b9a2d8")]
    [InlineData(RecentItemType.Recipe, "ce65df28-cbc2-4a2d-98b2-906064b9a2d8")]
    [InlineData(RecentItemType.Product, "00000000-0000-0000-0000-000000000000")]
    [InlineData(RecentItemType.Recipe, "00000000-0000-0000-0000-000000000000")]
    public void FromFields_PreservesStoredKindAndIdentityIncludingLegacyEmptySentinels(RecentItemType kind, string storedId) {
        var id = Guid.Parse(storedId);

        var reference = RecentItemReference.FromFields(kind, id);

        Assert.Multiple(
            () => Assert.Equal(kind, reference.Kind),
            () => Assert.Equal(id, reference.Id));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    public void FromFields_RejectsUndefinedKindsBeforeTypedReferenceDispatch(int kind) {
        ArgumentOutOfRangeException failure = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecentItemReference.FromFields((RecentItemType)kind, Guid.Empty));

        Assert.Equal("itemType", failure.ParamName);
    }

}
