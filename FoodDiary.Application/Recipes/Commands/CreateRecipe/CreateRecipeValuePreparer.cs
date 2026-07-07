using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

internal static class CreateRecipeValuePreparer {
    public static async Task<Result<CreateRecipeValues>> PrepareAsync(
        CreateRecipeCommand command,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<CreateRecipeValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<ImageAssetResolution> imageAssetResult = await ImageAssetResolver.ResolveOptionalAsync(
            command.ImageAssetId,
            nameof(command.ImageAssetId),
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateRecipeValues>(imageAssetResult.Error);
        }

        Result<Visibility> visibilityResult = EnumValueParser.ParseRequired<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<CreateRecipeValues>(visibilityResult.Error);
        }

        return Result.Success(new CreateRecipeValues(
            userId,
            visibilityResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageAsset));
    }
}
