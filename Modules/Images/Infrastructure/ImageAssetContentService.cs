using Amazon.S3;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using FoodDiary.Results;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Images.Infrastructure;

public sealed class ImageAssetContentService(
    IImageAssetReadRepository images,
    IObjectStorageClient storage,
    IOptions<S3Options> options) : IImageAssetContentService {
    public async Task<Result<string>> GetDataUrlAsync(
        ImageAssetId assetId, UserId userId, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ImageAsset? asset = await images.GetOwnedByIdAsync(assetId, userId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<string>(ImageErrors.NotFound(assetId.Value));
        }
        if (!asset.IsConfirmed) {
            return Result.Failure<string>(ImageErrors.InvalidData("Image upload has not been confirmed."));
        }
        if (!S3Options.HasCompleteConfiguration(options.Value)) {
            return Result.Failure<string>(ImageErrors.StorageError());
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try {
            return await ReadDataUrlAsync(asset.ObjectKey, deadline.Token).ConfigureAwait(false);
        } catch (InvalidDataException) {
            return Result.Failure<string>(ImageErrors.InvalidData("Stored image exceeds the allowed size."));
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<string>(ImageErrors.StorageError());
        } catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException) {
            return Result.Failure<string>(ImageErrors.StorageError());
        }
    }

    private async Task<Result<string>> ReadDataUrlAsync(string objectKey, CancellationToken cancellationToken) {
        S3Options settings = options.Value;
        long maximumBytes = Math.Min(settings.MaxUploadSizeBytes, S3Options.MaximumUploadSizeBytes);
        StoredObjectInfo? info = await storage.GetObjectInfoAsync(settings.Bucket, objectKey, cancellationToken).ConfigureAwait(false);
        string? contentType = info?.ContentType?.ToLowerInvariant();
        if (info is null || info.SizeBytes <= 0 || info.SizeBytes > maximumBytes
            || contentType is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif")) {
            return Result.Failure<string>(ImageErrors.InvalidData("Stored image is missing or has an unsupported size or content type."));
        }

        byte[]? content = await storage.GetObjectBytesAsync(settings.Bucket, objectKey, maximumBytes, cancellationToken).ConfigureAwait(false);
        if (content is null || content.LongLength != info.SizeBytes || content.LongLength > maximumBytes) {
            return Result.Failure<string>(ImageErrors.InvalidData("Stored image content is missing or incomplete."));
        }
        return Result.Success($"data:{contentType};base64,{Convert.ToBase64String(content)}");
    }
}
