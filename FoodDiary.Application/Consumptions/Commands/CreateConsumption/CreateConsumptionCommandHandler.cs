using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public class CreateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemRepository recentItemRepository,
    IUserRepository userRepository,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionModel>> {
    private sealed record CreateConsumptionValues(
        UserId UserId,
        MealType? MealType,
        ImageAssetId? ImageAssetId,
        ImageAsset? ImageAsset);

    public async Task<Result<ConsumptionModel>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken) {
        Result<CreateConsumptionValues> valuesResult = await PrepareCreateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(valuesResult.Error);
        }

        CreateConsumptionValues values = valuesResult.Value;
        var meal = Meal.Create(
            values.UserId,
            command.Date,
            values.MealType,
            command.Comment,
            values.ImageAsset?.Url ?? command.ImageUrl,
            values.ImageAssetId);
        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);

        Result itemsResult = ConsumptionManualItemAppender.Add(meal, command.Items);
        if (itemsResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(itemsResult.Error);
        }

        Result aiSessionsResult = await ConsumptionAiSessionAppender.AddAsync(
            meal,
            command.AiSessions,
            values.UserId,
            imageAssetAccessService,
            dateTimeProvider,
            cancellationToken).ConfigureAwait(false);
        if (aiSessionsResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(aiSessionsResult.Error);
        }

        Result nutritionResult = await ConsumptionNutritionApplier.ApplyAsync(
            meal,
            values.UserId,
            mealNutritionService,
            CreateNutritionInput(command),
            cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(nutritionResult.Error);
        }

        return await SaveAsync(meal, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<CreateConsumptionValues>> PrepareCreateValuesAsync(
        CreateConsumptionCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CreateConsumptionValues>(Errors.Authentication.InvalidToken);
        }

        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<CreateConsumptionValues>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CreateConsumptionValues>(accessError);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateConsumptionValues>(imageAssetResult.Error);
        }

        Result<MealType?> mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<CreateConsumptionValues>(mealTypeResult.Error);
        }

        return Result.Success(new CreateConsumptionValues(
            userId,
            mealTypeResult.Value,
            imageAssetIdResult.Value,
            imageAssetResult.Value));
    }

    private static ConsumptionNutritionInput CreateNutritionInput(CreateConsumptionCommand command) =>
        new(
            command.IsNutritionAutoCalculated,
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);

    private async Task<Result<ConsumptionModel>> SaveAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken) {
        await mealRepository.AddAsync(meal, cancellationToken).ConfigureAwait(false);
        await recentItemRepository.RegisterUsageAsync(
            userId,
            meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(meal.ToModel());
    }

}
