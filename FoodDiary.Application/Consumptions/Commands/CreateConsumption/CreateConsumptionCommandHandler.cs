using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public class CreateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IProductRepository productRepository,
    IRecipeRepository recipeRepository,
    IRecentItemRepository recentItemRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);

        var hasManualItems = command.Items is { Count: > 0 };
        var hasAiItems = command.AiSessions is { Count: > 0 } && command.AiSessions.Any(session => session.Items.Count > 0);
        if (!hasManualItems && !hasAiItems) {
            return Result.Failure<ConsumptionModel>(Errors.Validation.Required("Items"));
        }

        var mealTypeResult = ParseMealType(command.MealType);
        if (mealTypeResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(mealTypeResult.Error);
        }

        var meal = Meal.Create(userId, command.Date, mealTypeResult.Value, command.Comment, command.ImageUrl,
            command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null);

        var satietyValidation = SatietyLevelValidator.Validate(
            command.PreMealSatietyLevel,
            command.PostMealSatietyLevel);

        if (satietyValidation.IsFailure) {
            return Result.Failure<ConsumptionModel>(satietyValidation.Error);
        }

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);

        foreach (var item in command.Items) {
            var validation = ConsumptionItemValidator.Validate(item);
            if (validation.IsFailure) {
                return Result.Failure<ConsumptionModel>(validation.Error);
            }

            if (item.ProductId.HasValue) {
                meal.AddProduct(new ProductId(item.ProductId.Value), item.Amount);
            } else if (item.RecipeId.HasValue) {
                meal.AddRecipe(new RecipeId(item.RecipeId.Value), item.Amount);
            }
        }

        foreach (var session in command.AiSessions) {
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
                session.ImageAssetId.HasValue ? new ImageAssetId(session.ImageAssetId.Value) : null,
                session.RecognizedAtUtc ?? dateTimeProvider.UtcNow,
                session.Notes,
                sessionItems);
        }

        if (command.IsNutritionAutoCalculated) {
            var nutritionResult = await CalculateNutritionAsync(meal, userId, cancellationToken);
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

    private async Task<Result<MealNutritionSummary>> CalculateNutritionAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken) {
        var productIds = meal.Items
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var recipeIds = meal.Items
            .Where(i => i.RecipeId.HasValue)
            .Select(i => i.RecipeId!.Value)
            .Distinct()
            .ToList();

        var products = await productRepository.GetByIdsAsync(productIds, userId, includePublic: true, cancellationToken);
        if (products.Count != productIds.Count) {
            var missingProduct = productIds.First(id => !products.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Product.NotAccessible(missingProduct.Value));
        }

        var recipes = await recipeRepository.GetByIdsAsync(recipeIds, userId, includePublic: true, cancellationToken);
        if (recipes.Count != recipeIds.Count) {
            var missingRecipe = recipeIds.First(id => !recipes.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Recipe.NotAccessible(missingRecipe.Value));
        }

        var summary = MealNutritionCalculator.Calculate(meal, products, recipes);
        return Result.Success(summary);
    }
}
