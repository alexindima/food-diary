using FoodDiary.Modules.Meals.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;

using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Application.Common;

namespace FoodDiary.Modules.Meals.Application.Commands.CreateMeal;

public sealed class CreateMealCommandHandler(
    IMealWriteRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemUsageRecorder recentItemUsageRecorder,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService,
    IMealAchievementEvaluationRequest achievementEvaluationOutbox)
    : ICommandHandler<CreateMealCommand, Result<MealModel>> {
    public CreateMealCommandHandler(
        IMealWriteRepository mealRepository,
        IMealNutritionService mealNutritionService,
        IRecentItemUsageRecorder recentItemUsageRecorder,
        ICurrentUserAccessService currentUserAccessService,
        TimeProvider dateTimeProvider,
        IImageAssetAccessService imageAssetAccessService)
        : this(mealRepository, mealNutritionService, recentItemUsageRecorder, currentUserAccessService,
            dateTimeProvider, imageAssetAccessService, NullAchievementEvaluationOutbox.Instance) {
    }

    public async Task<Result<MealModel>> Handle(CreateMealCommand command, CancellationToken cancellationToken) {
        Result<CreateMealValues> valuesResult = await CreateMealValuePreparer.PrepareAsync(
            command,
            currentUserAccessService,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<MealModel>(valuesResult.Error);
        }

        CreateMealValues values = valuesResult.Value;
        var meal = Meal.Create(
            values.UserId,
            command.Date,
            values.MealType,
            command.Comment,
            values.ImageAsset?.Url ?? command.ImageUrl,
            values.ImageAssetId);
        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);

        Result itemsResult = MealManualItemAppender.Add(meal, command.Items);
        if (itemsResult.IsFailure) {
            return Result.Failure<MealModel>(itemsResult.Error);
        }

        Result aiSessionsResult = await MealAiSessionAppender.AddAsync(
            meal,
            command.AiSessions,
            values.UserId,
            imageAssetAccessService,
            dateTimeProvider,
            cancellationToken).ConfigureAwait(false);
        if (aiSessionsResult.IsFailure) {
            return Result.Failure<MealModel>(aiSessionsResult.Error);
        }

        Result nutritionResult = await MealNutritionApplier.ApplyAsync(
            meal,
            values.UserId,
            mealNutritionService,
            CreateNutritionInput(command),
            cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<MealModel>(nutritionResult.Error);
        }

        return await SaveAsync(meal, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private static MealNutritionInput CreateNutritionInput(CreateMealCommand command) =>
        new(
            command.IsNutritionAutoCalculated,
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);

    private async Task<Result<MealModel>> SaveAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken) {
        await mealRepository.AddAsync(meal, cancellationToken).ConfigureAwait(false);
        await achievementEvaluationOutbox.EnqueueAsync(userId, cancellationToken).ConfigureAwait(false);
        await recentItemUsageRecorder.RegisterUsageAsync(
            userId,
            meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(meal.ToModel());
    }

}
