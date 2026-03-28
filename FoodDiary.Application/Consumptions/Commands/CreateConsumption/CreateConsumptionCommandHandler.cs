using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Application.Recipes.Common;
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
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService,
    IRecentItemRepository recentItemRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);

        var mealType = string.IsNullOrWhiteSpace(command.MealType)
            ? (MealType?)null
            : Enum.Parse<MealType>(command.MealType, true);

        var meal = Meal.Create(userId, command.Date, mealType, command.Comment, command.ImageUrl,
            command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null);

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);

        foreach (var item in command.Items) {
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
            meal.ApplyNutrition(new MealNutritionUpdate(
                command.ManualCalories!.Value,
                command.ManualProteins!.Value,
                command.ManualFats!.Value,
                command.ManualCarbs!.Value,
                command.ManualFiber!.Value,
                command.ManualAlcohol ?? 0,
                IsAutoCalculated: false,
                ManualCalories: command.ManualCalories.Value,
                ManualProteins: command.ManualProteins.Value,
                ManualFats: command.ManualFats.Value,
                ManualCarbs: command.ManualCarbs.Value,
                ManualFiber: command.ManualFiber.Value,
                ManualAlcohol: command.ManualAlcohol ?? 0));
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

        var products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken);
        if (products.Count != productIds.Count) {
            var missingProduct = productIds.First(id => !products.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Product.NotAccessible(missingProduct.Value));
        }

        var recipes = await recipeLookupService.GetAccessibleByIdsAsync(recipeIds, userId, cancellationToken);
        if (recipes.Count != recipeIds.Count) {
            var missingRecipe = recipeIds.First(id => !recipes.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Recipe.NotAccessible(missingRecipe.Value));
        }

        var summary = MealNutritionCalculator.Calculate(meal, products, recipes);
        return Result.Success(summary);
    }
}
