using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.RepeatMeal;

public class RepeatMealCommandHandler(
    IMealRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IUserRepository userRepository)
    : ICommandHandler<RepeatMealCommand, Result<ConsumptionModel>> {
    private sealed record RepeatMealValues(
        UserId UserId,
        Meal SourceMeal,
        MealType? MealType);

    public async Task<Result<ConsumptionModel>> Handle(
        RepeatMealCommand command,
        CancellationToken cancellationToken) {
        Result<RepeatMealValues> valuesResult = await PrepareRepeatValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(valuesResult.Error);
        }

        RepeatMealValues values = valuesResult.Value;
        Meal newMeal = CreateRepeatedMeal(command, values);
        CopyItems(values.SourceMeal, newMeal);
        CopyAiSessions(values.SourceMeal, newMeal, command.TargetDate);
        await ApplyNutritionAsync(values.SourceMeal, newMeal, values.UserId, cancellationToken).ConfigureAwait(false);

        await mealRepository.AddAsync(newMeal, cancellationToken).ConfigureAwait(false);
        return Result.Success(newMeal.ToModel());
    }

    private async Task<Result<RepeatMealValues>> PrepareRepeatValuesAsync(
        RepeatMealCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RepeatMealValues>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<RepeatMealValues>(accessError);
        }

        var sourceMealId = new MealId(command.MealId);
        Meal? sourceMeal = await mealRepository.GetByIdAsync(
            sourceMealId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (sourceMeal is null) {
            return Result.Failure<RepeatMealValues>(Errors.Consumption.NotFound(command.MealId));
        }

        Result<MealType?> mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType ?? sourceMeal.MealType?.ToString(),
            "MealType",
            "Unknown meal type value.");
        return mealTypeResult.IsFailure
            ? Result.Failure<RepeatMealValues>(mealTypeResult.Error)
            : Result.Success(new RepeatMealValues(userId, sourceMeal, mealTypeResult.Value));
    }

    private static Meal CreateRepeatedMeal(RepeatMealCommand command, RepeatMealValues values) {
        Meal sourceMeal = values.SourceMeal;
        bool shouldCopyMealImage = !sourceMeal.AiSessions.Any(session => session.ImageAssetId.HasValue);
        return Meal.Create(
            values.UserId,
            command.TargetDate,
            values.MealType,
            sourceMeal.Comment,
            shouldCopyMealImage ? sourceMeal.ImageUrl : null,
            shouldCopyMealImage ? sourceMeal.ImageAssetId : null,
            sourceMeal.PreMealSatietyLevel,
            sourceMeal.PostMealSatietyLevel);
    }

    private static void CopyItems(Meal sourceMeal, Meal newMeal) {
        foreach (MealItem item in sourceMeal.Items) {
            MealItem? copiedItem = null;
            if (item.ProductId.HasValue) {
                copiedItem = newMeal.AddProduct(item.ProductId.Value, item.Amount);
            } else if (item.RecipeId.HasValue) {
                copiedItem = newMeal.AddRecipe(item.RecipeId.Value, item.Amount);
            }

            copiedItem?.CopySourceAndSnapshotFrom(item);
        }
    }

    private static void CopyAiSessions(Meal sourceMeal, Meal newMeal, DateTime targetDate) {
        DateTime targetRecognizedAtUtc = targetDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(targetDate, DateTimeKind.Utc)
            : targetDate.ToUniversalTime();

        foreach (MealAiSession session in sourceMeal.AiSessions) {
            var items = session.Items
                .Select(item => MealAiItemData.Create(
                    item.NameEn,
                    item.NameLocal,
                    item.Amount,
                    item.Unit,
                    item.Calories,
                    item.Proteins,
                    item.Fats,
                    item.Carbs,
                    item.Fiber,
                    item.Alcohol,
                    item.Confidence,
                    item.Resolution))
                .ToList();

            newMeal.AddAiSession(session.ImageAssetId, session.Source, targetRecognizedAtUtc, session.Notes, items);
        }
    }

    private async Task ApplyNutritionAsync(
        Meal sourceMeal,
        Meal newMeal,
        UserId userId,
        CancellationToken cancellationToken) {
        if (sourceMeal.IsNutritionAutoCalculated) {
            Result<MealNutritionSummary> nutritionResult = await mealNutritionService.CalculateAsync(newMeal, userId, cancellationToken).ConfigureAwait(false);
            if (nutritionResult.IsSuccess) {
                newMeal.ApplyNutrition(new MealNutritionUpdate(
                    nutritionResult.Value.Calories,
                    nutritionResult.Value.Proteins,
                    nutritionResult.Value.Fats,
                    nutritionResult.Value.Carbs,
                    nutritionResult.Value.Fiber,
                    nutritionResult.Value.Alcohol,
                    IsAutoCalculated: true));
            }

            return;
        }

        newMeal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: sourceMeal.TotalCalories,
            TotalProteins: sourceMeal.TotalProteins,
            TotalFats: sourceMeal.TotalFats,
            TotalCarbs: sourceMeal.TotalCarbs,
            TotalFiber: sourceMeal.TotalFiber,
            TotalAlcohol: sourceMeal.TotalAlcohol,
            IsAutoCalculated: false,
            ManualCalories: sourceMeal.ManualCalories,
            ManualProteins: sourceMeal.ManualProteins,
            ManualFats: sourceMeal.ManualFats,
            ManualCarbs: sourceMeal.ManualCarbs,
            ManualFiber: sourceMeal.ManualFiber,
            ManualAlcohol: sourceMeal.ManualAlcohol));
    }
}
