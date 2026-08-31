using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Meals.Mappings;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Meals.Common;

namespace FoodDiary.Application.Meals.Commands.CreateMeal;

public sealed class CreateMealCommandHandler(
    IMealWriteRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemUsageRecorder recentItemUsageRecorder,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService,
    IAchievementEvaluationOutbox achievementEvaluationOutbox)
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
