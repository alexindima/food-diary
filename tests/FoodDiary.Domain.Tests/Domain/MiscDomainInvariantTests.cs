using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
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

}
