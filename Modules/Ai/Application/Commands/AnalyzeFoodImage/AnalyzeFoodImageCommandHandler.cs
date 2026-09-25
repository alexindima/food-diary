using FoodDiary.Modules.Ai.Application.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

using FoodDiary.Modules.Images.Service.Contracts.Common;

namespace FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandHandler(
    IImageAssetContentService imageAssetContentService,
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

        if (!RecognitionImagesValidation.IsValid(query.ImageAssetId, query.IsProductLabel, query.AdditionalImageAssetIds)) {
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

        var additionalUrls = new List<string>();
        foreach (Guid id in query.AdditionalImageAssetIds ?? []) {
            Result<string> additional = await imageAssetContentService.GetDataUrlAsync((ImageAssetId)id, userId, cancellationToken).ConfigureAwait(false);
            if (additional.IsFailure) {
                return Result.Failure<FoodVisionModel>(additional.Error);
            }
            additionalUrls.Add(additional.Value);
        }
        return await openAiFoodService.AnalyzeFoodImageAsync(
            assetResult.Value,
            userId,
            query.Description,
            query.RequestId,
            cancellationToken, product: query.IsProductLabel ? new ProductImageAnalysis(additionalUrls) : null).ConfigureAwait(false);
    }
}
