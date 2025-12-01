using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var asset = new ImageAsset(ImageAssetId.New(), userId, objectKey, url);
        asset.SetCreated();
        return asset;
    }
}
