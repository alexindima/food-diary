using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserProfileMediaState(
    string? ProfileImage,
    ImageAssetId? ProfileImageAssetId) {
    public static UserProfileMediaState CreateInitial() {
        return new UserProfileMediaState(
            ProfileImage: null,
            ProfileImageAssetId: null);
    }
}
