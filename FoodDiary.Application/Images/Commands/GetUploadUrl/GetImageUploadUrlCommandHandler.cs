using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed class GetImageUploadUrlCommandHandler(
    IImageStorageService imageStorageService,
    IImageAssetRepository imageAssetRepository) : IRequestHandler<GetImageUploadUrlCommand, Result<GetImageUploadUrlResult>> {
    public async Task<Result<GetImageUploadUrlResult>> Handle(GetImageUploadUrlCommand request, CancellationToken cancellationToken) {
        var validationResult = ValidateRequest(request);
        if (validationResult.IsFailure) {
            return Result.Failure<GetImageUploadUrlResult>(validationResult.Error);
        }

        var userId = new UserId(request.UserId);

        PresignedUpload presign;
        try {
            presign = await imageStorageService.CreatePresignedUploadAsync(
                userId,
                request.FileName,
                request.ContentType,
                request.FileSizeBytes,
                cancellationToken);
        } catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException) {
            return Result.Failure<GetImageUploadUrlResult>(Errors.Image.InvalidData(ex.Message));
        }

        var asset = ImageAsset.Create(userId, presign.ObjectKey, presign.FileUrl);
        asset = await imageAssetRepository.AddAsync(asset, cancellationToken);

        return Result.Success(new GetImageUploadUrlResult(
            presign.UploadUrl,
            presign.FileUrl,
            presign.ObjectKey,
            presign.ExpirationUtc,
            asset.Id.Value));
    }

    private static Result ValidateRequest(GetImageUploadUrlCommand request) {
        if (request.UserId == Guid.Empty) {
            return Result.Failure(Errors.Image.InvalidData("UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.FileName)) {
            return Result.Failure(Errors.Image.InvalidData("File name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType)) {
            return Result.Failure(Errors.Image.InvalidData("Content type is required."));
        }

        if (request.FileSizeBytes <= 0) {
            return Result.Failure(Errors.Image.InvalidData("File size must be greater than zero."));
        }

        return Result.Success();
    }
}
