using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Ai.Models;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandHandler(
    IImageAssetRepository imageAssetRepository,
    IUserRepository userRepository,
    IOpenAiFoodService openAiFoodService)
    : IQueryHandler<AnalyzeFoodImageCommand, Result<FoodVisionModel>> {
    public async Task<Result<FoodVisionModel>> Handle(
        AnalyzeFoodImageCommand query,
        CancellationToken cancellationToken) {
        var asset = await imageAssetRepository.GetByIdAsync(query.ImageAssetId, cancellationToken);
        if (asset is null) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.ImageNotFound(query.ImageAssetId.Value));
        }

        if (asset.UserId != query.UserId) {
            return Result.Failure<FoodVisionModel>(Errors.Ai.Forbidden());
        }

        var user = await userRepository.GetByIdAsync(query.UserId);
        if (user is null) {
            return Result.Failure<FoodVisionModel>(Errors.User.NotFound(query.UserId.Value));
        }

        return await openAiFoodService.AnalyzeFoodImageAsync(
            asset.Url,
            user.Language,
            query.UserId,
            query.Description,
            cancellationToken);
    }
}
