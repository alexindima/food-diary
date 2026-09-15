using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserProfileMediaUpdate(
    string? ProfileImage = null,
    ImageAssetId? ProfileImageAssetId = null);
