using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Meals.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Commands.CreateMeal;

internal static class CreateMealValuePreparer {
    public static async Task<Result<CreateMealValues>> PrepareAsync(
        CreateMealCommand command,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<CreateMealValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<ImageAssetResolution> imageAssetResult = await ImageAssetResolver.ResolveOptionalAsync(
            command.ImageAssetId,
            nameof(command.ImageAssetId),
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateMealValues>(imageAssetResult.Error);
        }

        Result<MealType?> mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<CreateMealValues>(mealTypeResult.Error);
        }

        return Result.Success(new CreateMealValues(
            userId,
            mealTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageAsset));
    }
}
