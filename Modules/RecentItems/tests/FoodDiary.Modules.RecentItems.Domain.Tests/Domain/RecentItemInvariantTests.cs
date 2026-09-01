using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class RecentItemInvariantTests {
    [Fact]
    public void Create_WithEmptyItemId_Throws() => Assert.Throws<ArgumentException>(() =>
        RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.Empty));

    [Fact]
    public void Create_WithEmptyUserId_Throws() => Assert.Throws<ArgumentException>(() =>
        RecentItem.Create(UserId.Empty, RecentItemType.Product, Guid.NewGuid()));

    [Fact]
    public void Touch_IncrementsUsageCount() {
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid());
        int previousUsage = recentItem.UsageCount;
        recentItem.Touch();
        Assert.Equal(previousUsage + 1, recentItem.UsageCount);
        Assert.NotNull(recentItem.ModifiedOnUtc);
    }

    [Fact]
    public void Touch_WhenUsageCountAtMaxValue_DoesNotOverflow() {
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid());
        typeof(RecentItem).GetProperty(nameof(RecentItem.UsageCount), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(recentItem, int.MaxValue);
        recentItem.Touch();
        Assert.Equal(int.MaxValue, recentItem.UsageCount);
        Assert.NotNull(recentItem.ModifiedOnUtc);
    }

    [Fact]
    public void Touch_WithOlderTimestamp_DoesNotRegressLastUsedTime() {
        DateTime latestUsedAtUtc = DateTime.UtcNow;
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), latestUsedAtUtc);
        recentItem.Touch(latestUsedAtUtc.AddMinutes(-1));
        Assert.Multiple(
            () => Assert.Equal(latestUsedAtUtc, recentItem.LastUsedAtUtc),
            () => Assert.Equal(2, recentItem.UsageCount));
    }

    [Fact]
    public void Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTimestamp = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Local);
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), localTimestamp);
        Assert.Equal(localTimestamp.ToUniversalTime(), recentItem.LastUsedAtUtc);
        Assert.Equal(DateTimeKind.Utc, recentItem.LastUsedAtUtc.Kind);
    }

    [Fact]
    public void Create_WithUnspecifiedTimestamp_Throws() {
        var timestamp = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentOutOfRangeException>(() => RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), timestamp));
    }

    [Fact]
    public void Touch_WithUnspecifiedTimestamp_Throws() {
        var recentItem = RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.NewGuid(), DateTime.UtcNow);
        var timestamp = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentOutOfRangeException>(() => recentItem.Touch(timestamp));
    }
}
