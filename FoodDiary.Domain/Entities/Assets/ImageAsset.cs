using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Assets;

public sealed class ImageAsset : Entity<ImageAssetId>
{
    public UserId UserId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;

    // Navigation
    public User User { get; private set; } = null!;

    private ImageAsset() { }

    private ImageAsset(ImageAssetId id, UserId userId, string objectKey, string url)
        : base(id)
    {
        UserId = userId;
        ObjectKey = objectKey;
        Url = url;
    }

    public static ImageAsset Create(UserId userId, string objectKey, string url)
    {
        var normalizedObjectKey = NormalizeRequiredValue(objectKey, nameof(objectKey));
        var normalizedUrl = NormalizeRequiredValue(url, nameof(url));

        var asset = new ImageAsset(ImageAssetId.New(), userId, normalizedObjectKey, normalizedUrl);
        asset.SetCreated();
        return asset;
    }

    private static string NormalizeRequiredValue(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }
}

