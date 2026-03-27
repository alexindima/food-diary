using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Images.Common;
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
        if (user is null) {
            return Result.Failure<FoodVisionModel>(Errors.User.NotFound(userId));
        }

        return await openAiFoodService.AnalyzeFoodImageAsync(
            asset.Url,
            user.Language,
            userId,
            query.Description,
            cancellationToken);
    }
}
