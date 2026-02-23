using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using System.Reflection;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class MiscDomainInvariantTests {
    [Fact]
    public void Role_Create_WithBlankName_Throws() {
        Assert.Throws<ArgumentException>(() => Role.Create("   "));
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
        var previousUsage = recentItem.UsageCount;

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
    public void DailyAdvice_Update_NormalizesFields() {
        var advice = DailyAdvice.Create(" Hydrate ", " EN ", weight: 0, tag: "  water ");

        advice.Update(value: "  Sleep  ", locale: " RU ", weight: 0, tag: "   ");

        Assert.Equal("Sleep", advice.Value);
        Assert.Equal("ru", advice.Locale);
        Assert.Equal(1, advice.Weight);
        Assert.Null(advice.Tag);
    }

    [Fact]
    public void DailyAdvice_Create_WithLocaleVariant_NormalizesToPrimaryLanguage() {
        var advice = DailyAdvice.Create("Hydrate", "en-US");

        Assert.Equal("en", advice.Locale);
    }

    [Fact]
    public void DailyAdvice_Create_WithUnsupportedLocale_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DailyAdvice.Create("Hydrate", "de"));
    }

    [Fact]
    public void DailyAdvice_Update_WithSameNormalizedValues_DoesNotSetModifiedOnUtc() {
        var advice = DailyAdvice.Create("Hydrate", "en", weight: 1, tag: "water");

        advice.Update(value: "  Hydrate  ", locale: " EN ", weight: 1, tag: "  water  ");

        Assert.Null(advice.ModifiedOnUtc);
    }

    [Fact]
    public void DailyAdvice_Create_WithTooLongValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DailyAdvice.Create(new string('a', 513), "en"));
    }

    [Fact]
    public void DailyAdvice_Create_WithTooLongTag_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DailyAdvice.Create("Hydrate", "en", tag: new string('t', 65)));
    }
}
