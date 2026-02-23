using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed class GetImageUploadUrlCommandHandler(
    IImageStorageService imageStorageService,
    IImageAssetRepository imageAssetRepository) : IRequestHandler<GetImageUploadUrlCommand, GetImageUploadUrlResult> {
    public async Task<GetImageUploadUrlResult> Handle(GetImageUploadUrlCommand request, CancellationToken cancellationToken) {
        ValidateRequest(request);

        var presign = await imageStorageService.CreatePresignedUploadAsync(
            request.UserId,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            cancellationToken);

        var asset = ImageAsset.Create(request.UserId, presign.ObjectKey, presign.FileUrl);
        asset = await imageAssetRepository.AddAsync(asset, cancellationToken);

        return new GetImageUploadUrlResult(
            presign.UploadUrl,
            presign.FileUrl,
            presign.ObjectKey,
            presign.ExpirationUtc,
            asset.Id);
    }

    private static void ValidateRequest(GetImageUploadUrlCommand request) {
        if (request.UserId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(request.UserId));
        }

        if (string.IsNullOrWhiteSpace(request.FileName)) {
            throw new ArgumentException("File name is required.", nameof(request.FileName));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType)) {
            throw new ArgumentException("Content type is required.", nameof(request.ContentType));
        }

        if (request.FileSizeBytes <= 0) {
            throw new ArgumentOutOfRangeException(nameof(request.FileSizeBytes), "File size must be greater than zero.");
        }
    }
}
