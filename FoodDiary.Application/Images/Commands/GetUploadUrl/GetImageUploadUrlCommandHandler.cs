using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed class GetImageUploadUrlCommandHandler(
    IImageStorageService imageStorageService,
    IImageAssetWriteRepository imageAssetRepository) : ICommandHandler<GetImageUploadUrlCommand, Result<GetImageUploadUrlResult>> {
    public async Task<Result<GetImageUploadUrlResult>> Handle(GetImageUploadUrlCommand request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = ValidateRequest(request);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<GetImageUploadUrlResult>(userIdResult);
        }

        UserId userId = userIdResult.Value;

        PresignedUpload presign;
        try {
            presign = await imageStorageService.CreatePresignedUploadAsync(
                userId,
                request.FileName,
                request.ContentType,
                request.FileSizeBytes,
                cancellationToken).ConfigureAwait(false);
        } catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException) {
            return Result.Failure<GetImageUploadUrlResult>(Errors.Image.InvalidData(ex.Message));
        }

        var asset = ImageAsset.Create(userId, presign.ObjectKey, presign.FileUrl);
        asset = await imageAssetRepository.AddAsync(asset, cancellationToken).ConfigureAwait(false);

        return Result.Success(new GetImageUploadUrlResult(
            presign.UploadUrl,
            presign.FileUrl,
            presign.ExpirationUtc,
            asset.Id.Value));
    }

    private static Result<UserId> ValidateRequest(GetImageUploadUrlCommand request) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            request.UserId,
            Errors.Image.InvalidData("UserId is required."));
        if (userIdResult.IsFailure) {
            return userIdResult;
        }

        if (string.IsNullOrWhiteSpace(request.FileName)) {
            return Result.Failure<UserId>(Errors.Image.InvalidData("File name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType)) {
            return Result.Failure<UserId>(Errors.Image.InvalidData("Content type is required."));
        }

        if (request.FileSizeBytes <= 0) {
            return Result.Failure<UserId>(Errors.Image.InvalidData("File size must be greater than zero."));
        }

        return userIdResult;
    }
}
