using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
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

        var presign = await imageStorageService.CreatePresignedUploadAsync(
            request.UserId,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            cancellationToken);

        var asset = ImageAsset.Create(request.UserId, presign.ObjectKey, presign.FileUrl);
        asset = await imageAssetRepository.AddAsync(asset, cancellationToken);

        return Result.Success(new GetImageUploadUrlResult(
            presign.UploadUrl,
            presign.FileUrl,
            presign.ObjectKey,
            presign.ExpirationUtc,
            asset.Id));
    }

    private static Result ValidateRequest(GetImageUploadUrlCommand request) {
        if (request.UserId == UserId.Empty) {
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
