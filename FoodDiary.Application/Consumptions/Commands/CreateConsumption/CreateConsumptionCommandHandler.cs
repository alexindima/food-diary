using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public sealed class CreateConsumptionCommandHandler(
    IMealWriteRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemWriteRepository recentItemRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken) {
        Result<CreateConsumptionValues> valuesResult = await CreateConsumptionValuePreparer.PrepareAsync(
            command,
            currentUserAccessService,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
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
