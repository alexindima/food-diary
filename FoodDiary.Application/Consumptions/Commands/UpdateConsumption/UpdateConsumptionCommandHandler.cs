using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Users.Common;
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
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(UpdateConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        if (command.ConsumptionId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(
                Errors.Validation.Invalid(nameof(command.ConsumptionId), "Consumption id must not be empty."));
        }

        var imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ConsumptionModel>(accessError);
        }

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

        var mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
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
            var sessionImageAssetIdResult = ImageAssetIdParser.ParseOptional(session.ImageAssetId, nameof(session.ImageAssetId));
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

            var sessionSource = Enum.TryParse<AiRecognitionSource>(session.Source, true, out var parsedSource)
                ? parsedSource
                : AiRecognitionSource.Text;

            meal.AddAiSession(
                sessionImageAssetIdResult.Value,
                sessionSource,
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

        var imageAssetChanged = command.ImageAssetId.HasValue &&
                                oldAssetId.HasValue &&
                                oldAssetId.Value.Value != command.ImageAssetId.Value;

        if (oldAssetId.HasValue && imageAssetChanged) {
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
    private static Result ValidateItemIdentifiers(ConsumptionItemInput item) {
        var productIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.ProductId, nameof(item.ProductId), "Product id");
        if (productIdResult.IsFailure) {
            return productIdResult;
        }

        var recipeIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.RecipeId, nameof(item.RecipeId), "Recipe id");
        if (recipeIdResult.IsFailure) {
            return recipeIdResult;
        }

        return Result.Success();
    }
}
