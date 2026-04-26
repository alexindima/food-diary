using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandHandler(
    IImageAssetRepository imageAssetRepository,
    IUserRepository userRepository,
    IOpenAiFoodService openAiFoodService)
    : IQueryHandler<AnalyzeFoodImageCommand, Result<FoodVisionModel>> {
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
        var asset = await imageAssetRepository.GetByIdAsync(imageAssetId, cancellationToken);
        if (asset is null) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.ImageNotFound(query.ImageAssetId));
        }

        if (asset.UserId != userId) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.Forbidden());
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<FoodVisionModel>(accessError);
        }

        var currentUser = user!;
        return await openAiFoodService.AnalyzeFoodImageAsync(
            asset.Url,
            currentUser.Language,
            userId,
            query.Description,
            cancellationToken);
    }
}
