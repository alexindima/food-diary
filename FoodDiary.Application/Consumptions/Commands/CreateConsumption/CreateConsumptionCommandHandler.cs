using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public class CreateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemRepository recentItemRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        var imageAssetIdResult = NormalizeImageAssetId(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        var mealTypeResult = ParseMealType(command.MealType);
        if (mealTypeResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(mealTypeResult.Error);
        }

        var meal = Meal.Create(userId, command.Date, mealTypeResult.Value, command.Comment, command.ImageUrl,
            imageAssetIdResult.Value);

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);

        foreach (var item in command.Items) {
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

        await mealRepository.AddAsync(meal, cancellationToken);
        await recentItemRepository.RegisterUsageAsync(
            userId,
            meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken);

        var created = await mealRepository.GetByIdAsync(
            meal.Id,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken);

        return created is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.InvalidData("Failed to load created consumption."))
            : Result.Success(created.ToModel());
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
