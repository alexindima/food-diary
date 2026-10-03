using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserProfileMediaUpdateTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateProfileMedia_NullClearsBothImageFieldsOnlyWhenSpecified(bool specified) {
        var user = User.Create("avatar@example.com", "hash");
        var assetId = ImageAssetId.New();
        const string originalImage = "https://example.test/avatar.png";
        user.UpdateProfileMedia(new UserProfileMediaUpdate(originalImage, assetId));
        user.UpdatePersonalInfo(firstName: "QA", height: 170);

        user.UpdateProfileMedia(new UserProfileMediaUpdate(ProfileImageSpecified: specified));

        Assert.Multiple(
            () => Assert.Equal(specified ? null : originalImage, user.ProfileImage),
            () => Assert.Equal(specified ? null : (ImageAssetId?)assetId, user.ProfileImageAssetId),
            () => Assert.Equal("QA", user.FirstName),
            () => Assert.Equal(170, user.HeightCm));
    }

    [Fact]
    public void UpdateProfileMedia_SelectedImageStillUpdatesWithoutPresenceMetadata() {
        var user = User.Create("avatar@example.com", "hash");
        var assetId = ImageAssetId.New();

        user.UpdateProfileMedia(new UserProfileMediaUpdate("https://example.test/avatar.png", assetId));

        Assert.Multiple(
            () => Assert.Equal("https://example.test/avatar.png", user.ProfileImage),
            () => Assert.Equal(assetId, user.ProfileImageAssetId));
    }
}
