using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Domain.Entities.Assets;

public sealed class ImageAsset : Entity<ImageAssetId> {
    public UserId UserId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public bool IsConfirmed { get; private set; }
    public bool IsPrivate { get; private set; }

    private ImageAsset() {
    }

    private ImageAsset(ImageAssetId id, UserId userId, string objectKey, string url, bool isPrivate)
        : base(id) {
        UserId = userId;
        ObjectKey = objectKey;
        Url = url;
        IsPrivate = isPrivate;
    }

    public static ImageAsset CreateWithLocation(UserId userId, ObjectStorageKey objectKey, PublicImageUrl url, bool keepPrivate = false) {
        EnsureUserId(userId);
        ArgumentNullException.ThrowIfNull(objectKey);
        ArgumentNullException.ThrowIfNull(url);
        return Create(userId, objectKey.Value, url.Value, keepPrivate);
    }

    public static ImageAsset Create(UserId userId, string objectKey, string url, bool keepPrivate = false) {
        EnsureUserId(userId);
        string normalizedObjectKey = NormalizeRequiredValue(objectKey, nameof(objectKey));
        string normalizedUrl = NormalizeRequiredValue(url, nameof(url));

        var asset = new ImageAsset(ImageAssetId.New(), userId, normalizedObjectKey, normalizedUrl, keepPrivate);
        asset.SetCreated();
        return asset;
    }

    public void Confirm() => IsConfirmed = true;

    public void Promote() {
        if (!IsConfirmed) {
            throw new InvalidOperationException("Only a confirmed image can be promoted.");
        }

        IsPrivate = false;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static string NormalizeRequiredValue(string value, string paramName) {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", paramName)
            : value.Trim();
    }
}
