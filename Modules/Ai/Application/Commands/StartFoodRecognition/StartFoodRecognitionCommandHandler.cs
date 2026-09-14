using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Images.Models;

using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;

public sealed class StartFoodRecognitionCommandHandler(
    IFoodRecognitionJobStore store,
    IImageAssetAccessService images,
    IUserAiProfileReadService userContext,
    TimeProvider timeProvider) : ICommandHandler<StartFoodRecognitionCommand, Result<FoodRecognitionJobModel>> {
    public async Task<Result<FoodRecognitionJobModel>> Handle(StartFoodRecognitionCommand request, CancellationToken cancellationToken) {
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
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await store.CreateAsync(new FoodRecognitionJobModel(
            request.Id, request.UserId, request.ImageAssetId, image.Value.Url, request.Description,
            "Queued", now, now), cancellationToken).ConfigureAwait(false);
    }
}
