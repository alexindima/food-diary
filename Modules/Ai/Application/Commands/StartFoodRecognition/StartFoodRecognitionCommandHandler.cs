using FoodDiary.Modules.Ai.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;

using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;

public sealed class StartFoodRecognitionCommandHandler(
    IFoodRecognitionJobStore store,
    IImageAssetAccessService images,
    IUserAiProfileReadService userContext,
    TimeProvider timeProvider) : ICommandHandler<StartFoodRecognitionCommand, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(StartFoodRecognitionCommand request, CancellationToken cancellationToken) {
        if (!RecognitionImagesValidation.IsValid(request.ImageAssetId, request.IsProductLabel, request.AdditionalImageAssetIds)) {
            return Result.Failure<FoodRecognitionJobModel>(Errors.Validation.Invalid("Images", "Invalid product image selection."));
        }
        var userId = (UserId)request.UserId;
        Result<UserAiProfileModel> context = await userContext.GetAiProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (context.IsFailure) {
            return Result.Failure<FoodRecognitionJobModel>(context.Error);
        }
        if (!context.Value.HasAcceptedAiConsent) {
            return Result.Failure<FoodRecognitionJobModel>(AiErrors.ConsentRequired());
        }
        Result<ImageAssetReadModel?> image = await images.ResolveOptionalAsync((ImageAssetId)request.ImageAssetId, userId, cancellationToken).ConfigureAwait(false);
        if (image.IsFailure) {
            return Result.Failure<FoodRecognitionJobModel>(image.Error);
        }
        if (image.Value is null) {
            return Result.Failure<FoodRecognitionJobModel>(AiErrors.ImageNotFound(request.ImageAssetId));
        }
        var additionalImages = new List<FoodRecognitionImageModel>();
        foreach (Guid id in request.AdditionalImageAssetIds ?? []) {
            Result<ImageAssetReadModel?> additional = await images.ResolveOptionalAsync((ImageAssetId)id, userId, cancellationToken).ConfigureAwait(false);
            if (additional.IsFailure) {
                return Result.Failure<FoodRecognitionJobModel>(additional.Error);
            }
            if (additional.Value is null) {
                return Result.Failure<FoodRecognitionJobModel>(AiErrors.ImageNotFound(id));
            }
            additionalImages.Add(new FoodRecognitionImageModel(id, additional.Value.Url));
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await store.CreateAsync(new FoodRecognitionJobModel(
            request.Id, request.UserId, request.ImageAssetId, image.Value.Url, request.Description,
            "Queued", now, now, IsProductLabel: request.IsProductLabel, AdditionalImages: additionalImages), cancellationToken).ConfigureAwait(false);
    }
}
