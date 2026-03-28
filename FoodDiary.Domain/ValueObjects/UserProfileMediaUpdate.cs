using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserProfileMediaUpdate(
    string? ProfileImage = null,
    ImageAssetId? ProfileImageAssetId = null);
