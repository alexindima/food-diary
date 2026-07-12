using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Assets;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandHandler(
    IImageAssetAccessService imageAssetAccessService,
    IAiUserContextService aiUserContextService,
    IOpenAiFoodService openAiFoodService)
    : ICommandHandler<AnalyzeFoodImageCommand, Result<FoodVisionModel>> {
    public async Task<Result<FoodVisionModel>> Handle(
        AnalyzeFoodImageCommand query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            query.UserId,
            Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<FoodVisionModel>(userIdResult);
        }

        Result<ImageAssetId> imageAssetIdResult = RequiredIdParser.Parse(
            query.ImageAssetId,
            nameof(query.ImageAssetId),
            "Image asset id must not be empty.",
            value => new ImageAssetId(value));
        if (imageAssetIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<FoodVisionModel, ImageAssetId>(imageAssetIdResult);
        }

        UserId userId = userIdResult.Value;
        ImageAssetId imageAssetId = imageAssetIdResult.Value;
        Result<ImageAsset?> assetResult = await imageAssetAccessService
            .ResolveOptionalAsync(imageAssetId, userId, cancellationToken)
            .ConfigureAwait(false);
        if (assetResult.IsFailure) {
            Error error = assetResult.Error.Code switch {
                "Image.NotFound" => Errors.Ai.ImageNotFound(query.ImageAssetId),
                "Image.Forbidden" => Errors.Ai.Forbidden(),
                _ => assetResult.Error,
            };
            return Result.Failure<FoodVisionModel>(error);
        }

        ImageAsset asset = assetResult.Value!;

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
