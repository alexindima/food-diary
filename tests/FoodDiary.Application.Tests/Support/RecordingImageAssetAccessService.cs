using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecordingImageAssetAccessService : IImageAssetAccessService {
    private readonly Dictionary<ImageAssetId, string> _urls = [];
    private readonly List<ImageAssetId?> _requestedAssetIds = [];
    private Error? _failure;

    public IReadOnlyList<ImageAssetId?> RequestedAssetIds => _requestedAssetIds;

    public RecordingImageAssetAccessService WithAsset(ImageAssetId assetId, string url) {
        _urls[assetId] = url;
        return this;
    }

    public RecordingImageAssetAccessService WithFailure(Error error) {
        _failure = error;
        return this;
    }

    public Task<Result<ImageAsset?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        _requestedAssetIds.Add(assetId);
        if (_failure is not null) {
            return Task.FromResult(Result.Failure<ImageAsset?>(_failure));
        }

        if (!assetId.HasValue) {
            return Task.FromResult(Result.Success<ImageAsset?>(null));
        }

        string url = _urls.GetValueOrDefault(assetId.Value, $"https://cdn.example/{assetId.Value.Value:D}.jpg");
        var asset = ImageAsset.Create(userId, $"images/{assetId.Value.Value:D}.jpg", url);
        return Task.FromResult(Result.Success<ImageAsset?>(asset));
    }
}
