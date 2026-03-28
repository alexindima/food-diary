using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

public class UpdateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemRepository recentItemRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(UpdateConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        var imageAssetIdResult = NormalizeImageAssetId(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        var consumptionId = new MealId(command.ConsumptionId);

        var meal = await mealRepository.GetByIdAsync(
            consumptionId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (meal is null) {
            return Result.Failure<ConsumptionModel>(Errors.Consumption.NotFound(command.ConsumptionId));
        }

        var hasManualItems = command.Items is { Count: > 0 };
        var hasAiItems = command.AiSessions is { Count: > 0 } && command.AiSessions.Any(session => session.Items.Count > 0);
        if (!hasManualItems && !hasAiItems) {
            return Result.Failure<ConsumptionModel>(Errors.Validation.Required("Items"));
        }

        var mealTypeResult = ParseMealType(command.MealType);
        if (mealTypeResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(mealTypeResult.Error);
        }

        var oldAssetId = meal.ImageAssetId;

        meal.UpdateDate(command.Date);
        meal.UpdateMealType(mealTypeResult.Value);
        meal.UpdateComment(command.Comment);
        meal.UpdateImage(command.ImageUrl, imageAssetIdResult.Value);

        var satietyValidation = SatietyLevelValidator.Validate(
            command.PreMealSatietyLevel,
            command.PostMealSatietyLevel);

        if (satietyValidation.IsFailure) {
            return Result.Failure<ConsumptionModel>(satietyValidation.Error);
        }

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);
        meal.ClearItems();
        meal.ClearAiSessions();

        foreach (var item in command.Items) {
            var validation = ConsumptionItemValidator.Validate(item);
            if (validation.IsFailure) {
                return Result.Failure<ConsumptionModel>(validation.Error);
            }

            var itemIdValidation = ValidateItemIdentifiers(item);
            if (itemIdValidation.IsFailure) {
                return Result.Failure<ConsumptionModel>(itemIdValidation.Error);
            }

            if (item.ProductId.HasValue) {
                meal.AddProduct(new ProductId(item.ProductId.Value), item.Amount);
            } else if (item.RecipeId.HasValue) {
                meal.AddRecipe(new RecipeId(item.RecipeId.Value), item.Amount);
            }
        }

        foreach (var session in command.AiSessions) {
            var sessionImageAssetIdResult = NormalizeImageAssetId(session.ImageAssetId, nameof(session.ImageAssetId));
            if (sessionImageAssetIdResult.IsFailure) {
                return Result.Failure<ConsumptionModel>(sessionImageAssetIdResult.Error);
            }

            var sessionItems = session.Items
                .Select(aiItem => MealAiItemData.Create(
                    aiItem.NameEn,
                    aiItem.NameLocal,
                    aiItem.Amount,
                    aiItem.Unit,
                    aiItem.Calories,
                    aiItem.Proteins,
                    aiItem.Fats,
                    aiItem.Carbs,
                    aiItem.Fiber,
                    aiItem.Alcohol))
                .ToList();

            meal.AddAiSession(
                sessionImageAssetIdResult.Value,
                session.RecognizedAtUtc ?? dateTimeProvider.UtcNow,
                session.Notes,
                sessionItems);
        }

        if (command.IsNutritionAutoCalculated) {
            var nutritionResult = await mealNutritionService.CalculateAsync(meal, userId, cancellationToken);
            if (nutritionResult.IsFailure) {
                return Result.Failure<ConsumptionModel>(nutritionResult.Error);
            }

            meal.ApplyNutrition(new MealNutritionUpdate(
                nutritionResult.Value.Calories,
                nutritionResult.Value.Proteins,
                nutritionResult.Value.Fats,
                nutritionResult.Value.Carbs,
                nutritionResult.Value.Fiber,
                nutritionResult.Value.Alcohol,
                IsAutoCalculated: true));
        } else {
            var manualNutritionResult = ManualNutritionValidator.Validate(
                command.ManualCalories,
                command.ManualProteins,
                command.ManualFats,
                command.ManualCarbs,
                command.ManualFiber,
                command.ManualAlcohol);

            if (manualNutritionResult.IsFailure) {
                return Result.Failure<ConsumptionModel>(manualNutritionResult.Error);
            }

            var manual = manualNutritionResult.Value;
            meal.ApplyNutrition(new MealNutritionUpdate(
                manual.Calories,
                manual.Proteins,
                manual.Fats,
                manual.Carbs,
                manual.Fiber,
                manual.Alcohol,
                IsAutoCalculated: false,
                ManualCalories: manual.Calories,
                ManualProteins: manual.Proteins,
                ManualFats: manual.Fats,
                ManualCarbs: manual.Carbs,
                ManualFiber: manual.Fiber,
                ManualAlcohol: manual.Alcohol));
        }

        await mealRepository.UpdateAsync(meal, cancellationToken);
        await recentItemRepository.RegisterUsageAsync(
            userId,
            meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken);

        if (oldAssetId.HasValue && (!command.ImageAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value)) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken);
        }

        var updated = await mealRepository.GetByIdAsync(
            meal.Id,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken);

        return updated is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.InvalidData("Failed to load updated consumption."))
            : Result.Success(updated.ToModel());
    }

    private static Result<MealType?> ParseMealType(string? mealType) {
        if (string.IsNullOrWhiteSpace(mealType)) {
            return Result.Success<MealType?>(null);
        }

        return Enum.TryParse<MealType>(mealType, true, out var parsed)
            ? Result.Success<MealType?>(parsed)
            : Result.Failure<MealType?>(Errors.Validation.Invalid(nameof(mealType), "Unknown meal type value."));
    }

    private static Result ValidateItemIdentifiers(ConsumptionItemInput item) {
        if (item.ProductId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(item.ProductId), "Product id must not be empty."));
        }

        if (item.RecipeId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(item.RecipeId), "Recipe id must not be empty."));
        }

        return Result.Success();
    }

    private static Result<ImageAssetId?> NormalizeImageAssetId(Guid? value, string fieldName) {
        if (!value.HasValue) {
            return Result.Success<ImageAssetId?>(null);
        }

        return value.Value == Guid.Empty
            ? Result.Failure<ImageAssetId?>(Errors.Validation.Invalid(fieldName, "Image asset id must not be empty."))
            : Result.Success<ImageAssetId?>(new ImageAssetId(value.Value));
    }
}
