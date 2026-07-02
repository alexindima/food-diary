using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Assets;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandHandler(
    IImageAssetRepository imageAssetRepository,
    IAiUserContextService aiUserContextService,
    IOpenAiFoodService openAiFoodService,
    IImageStorageService imageStorageService)
    : ICommandHandler<AnalyzeFoodImageCommand, Result<FoodVisionModel>> {
    public async Task<Result<FoodVisionModel>> Handle(
        AnalyzeFoodImageCommand query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<FoodVisionModel>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        if (query.ImageAssetId == Guid.Empty) {
            return Result.Failure<FoodVisionModel>(
                Errors.Validation.Invalid(nameof(query.ImageAssetId), "Image asset id must not be empty."));
        }

        var userId = new UserId(query.UserId);
        var imageAssetId = new ImageAssetId(query.ImageAssetId);
        ImageAsset? asset = await imageAssetRepository.GetByIdAsync(imageAssetId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.ImageNotFound(query.ImageAssetId));
        }

        if (asset.UserId != userId) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.Forbidden());
        }

        ImageObjectValidationResult imageValidation = await imageStorageService.ValidateUploadedObjectAsync(asset.ObjectKey, cancellationToken).ConfigureAwait(false);
        if (!imageValidation.IsValid) {
            return Result.Failure<FoodVisionModel>(Errors.Image.InvalidData(
                imageValidation.Message ?? "Image upload has not completed or is invalid."));
        }

        Result<AiUserContext> contextResult = await aiUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(contextResult.Error);
        }

        return await openAiFoodService.AnalyzeFoodImageAsync(
            asset.Url,
            contextResult.Value.Language,
            userId,
            query.Description,
            cancellationToken).ConfigureAwait(false);
    }
}
