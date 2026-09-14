using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

using FoodDiary.Application.Abstractions.Images.Common;

namespace FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandHandler(
    IImageAssetContentService imageAssetContentService,
    IUserAiProfileReadService userProfileReadService,
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

        if (query.ImageAssetId == Guid.Empty) {
            return Result.Failure<FoodVisionModel>(Errors.Validation.Invalid(
                nameof(query.ImageAssetId),
                "Image asset id must not be empty."));
        }

        UserId userId = userIdResult.Value;
        var imageAssetId = (ImageAssetId)query.ImageAssetId;
        Result<string> assetResult = await imageAssetContentService
            .GetDataUrlAsync(imageAssetId, userId, cancellationToken)
            .ConfigureAwait(false);
        if (assetResult.IsFailure) {
            Error error = assetResult.Error.Code switch {
                "Image.NotFound" => AiErrors.ImageNotFound(query.ImageAssetId),
                "Image.Forbidden" => AiErrors.Forbidden(),
                _ => assetResult.Error,
            };
            return Result.Failure<FoodVisionModel>(error);
        }

        Result<UserAiProfileModel> contextResult = await userProfileReadService.GetAiProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(contextResult.Error);
        }

        return await openAiFoodService.AnalyzeFoodImageAsync(
            assetResult.Value,
            contextResult.Value.Language,
            userId,
            query.Description,
            query.RequestId,
            cancellationToken).ConfigureAwait(false);
    }
}
