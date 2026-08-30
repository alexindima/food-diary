using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using System.Reflection;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class MiscDomainInvariantTests {
    [Fact]
    public void Role_Create_WithBlankName_Throws() {
        Assert.Throws<ArgumentException>(() => Role.Create("   "));
    }

    [Fact]
    public void Role_UserRoles_AreExposedAsReadOnly() {
        var role = Role.Create("admin");
        ICollection<UserRole> userRoles = Assert.IsAssignableFrom<ICollection<UserRole>>(role.UserRoles);

        Assert.True(userRoles.IsReadOnly);
    }

    [Fact]
    public void ImageAsset_Create_TrimsValues() {
        var asset = ImageAsset.Create(UserId.New(), "  images/a.png  ", "  https://cdn/a.png  ");

        Assert.Equal("images/a.png", asset.ObjectKey);
        Assert.Equal("https://cdn/a.png", asset.Url);
    }

    [Fact]
    public void ImageAsset_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ImageAsset.Create(UserId.Empty, "images/a.png", "https://cdn/a.png"));
    }

    [Fact]
    public void ImageAsset_Create_WithBlankObjectKey_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ImageAsset.Create(UserId.New(), "   ", "https://cdn/a.png"));
    }

    [Fact]
    public void ImageAsset_Create_WithBlankUrl_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ImageAsset.Create(UserId.New(), "images/a.png", "   "));
    }

    [Fact]
    public void RecentItem_Create_WithEmptyItemId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.Empty));
    }

    [Fact]
    public void RecentItem_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecentItem.Create(UserId.Empty, RecentItemType.Product, Guid.NewGuid()));
    }

    [Fact]
    public void RecentItem_Touch_IncrementsUsageCount() {
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid());
        int previousUsage = recentItem.UsageCount;

        recentItem.Touch();

        Assert.Equal(previousUsage + 1, recentItem.UsageCount);
        Assert.NotNull(recentItem.ModifiedOnUtc);
    }

    [Fact]
    public void RecentItem_Touch_WhenUsageCountAtMaxValue_DoesNotOverflow() {
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid());
        typeof(RecentItem)
            .GetProperty(nameof(RecentItem.UsageCount), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(recentItem, int.MaxValue);

        recentItem.Touch();

        Assert.Equal(int.MaxValue, recentItem.UsageCount);
        Assert.NotNull(recentItem.ModifiedOnUtc);
    }

    [Fact]
    public void RecentItem_Touch_WithOlderTimestamp_DoesNotRegressLastUsedTime() {
        DateTime latestUsedAtUtc = DateTime.UtcNow;
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), latestUsedAtUtc);

        recentItem.Touch(latestUsedAtUtc.AddMinutes(-1));

        Assert.Multiple(
            () => Assert.Equal(latestUsedAtUtc, recentItem.LastUsedAtUtc),
            () => Assert.Equal(2, recentItem.UsageCount));
    }

    [Fact]
    public void RecentItem_Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTimestamp = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Local);

        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), localTimestamp);

        Assert.Equal(localTimestamp.ToUniversalTime(), recentItem.LastUsedAtUtc);
        Assert.Equal(DateTimeKind.Utc, recentItem.LastUsedAtUtc.Kind);
    }

    [Fact]
    public void RecentItem_Create_WithUnspecifiedTimestamp_Throws() {
        var unspecifiedTimestamp = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), unspecifiedTimestamp));
    }

    [Fact]
    public void RecentItem_Touch_WithUnspecifiedTimestamp_Throws() {
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), DateTime.UtcNow);
        var unspecifiedTimestamp = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() => recentItem.Touch(unspecifiedTimestamp));
    }

}
