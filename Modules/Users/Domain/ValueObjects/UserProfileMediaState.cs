using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserProfileMediaState(
    string? ProfileImage,
    ImageAssetId? ProfileImageAssetId) {
    public static UserProfileMediaState CreateInitial() {
        return new UserProfileMediaState(
            ProfileImage: null,
            ProfileImageAssetId: null);
    }
}
