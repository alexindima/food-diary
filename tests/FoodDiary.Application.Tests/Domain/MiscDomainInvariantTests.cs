using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class MiscDomainInvariantTests
{
    [Fact]
    public void Role_Create_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Role.Create("   "));
    }

    [Fact]
    public void ImageAsset_Create_TrimsValues()
    {
        var asset = ImageAsset.Create(UserId.New(), "  images/a.png  ", "  https://cdn/a.png  ");

        Assert.Equal("images/a.png", asset.ObjectKey);
        Assert.Equal("https://cdn/a.png", asset.Url);
    }

    [Fact]
    public void RecentItem_Create_WithEmptyItemId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RecentItem.Create(UserId.New(), RecentItemType.Product, Guid.Empty));
    }

    [Fact]
    public void DailyAdvice_Update_NormalizesFields()
    {
        var advice = DailyAdvice.Create(" Hydrate ", " EN ", weight: 0, tag: "  water ");

        advice.Update(value: "  Sleep  ", locale: " RU ", weight: 0, tag: "   ");

        Assert.Equal("Sleep", advice.Value);
        Assert.Equal("ru", advice.Locale);
        Assert.Equal(1, advice.Weight);
        Assert.Null(advice.Tag);
    }
}

