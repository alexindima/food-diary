using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

public class UpdateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IProductRepository productRepository,
    IRecipeRepository recipeRepository)
    : ICommandHandler<UpdateConsumptionCommand, Result<ConsumptionResponse>>
{
    public async Task<Result<ConsumptionResponse>> Handle(UpdateConsumptionCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId is null || command.UserId == UserId.Empty)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Authentication.InvalidToken);
        }

        var meal = await mealRepository.GetByIdAsync(
            command.ConsumptionId,
            command.UserId.Value,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (meal is null)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Consumption.NotFound(command.ConsumptionId.Value));
        }

        if (command.Items is not { Count: > 0 })
        {
            return Result.Failure<ConsumptionResponse>(Errors.Validation.Required("Items"));
        }

        var mealTypeResult = ParseMealType(command.MealType);
        if (mealTypeResult.IsFailure)
        {
            return Result.Failure<ConsumptionResponse>(mealTypeResult.Error);
        }

        meal.UpdateDate(command.Date);
        meal.UpdateMealType(mealTypeResult.Value);
        meal.UpdateComment(command.Comment);

        var satietyValidation = SatietyLevelValidator.Validate(
            command.PreMealSatietyLevel,
            command.PostMealSatietyLevel);

        if (satietyValidation.IsFailure)
        {
            return Result.Failure<ConsumptionResponse>(satietyValidation.Error);
        }

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);
        meal.ClearItems();

        foreach (var item in command.Items)
        {
            var validation = ConsumptionItemValidator.Validate(item);
            if (validation.IsFailure)
            {
                return Result.Failure<ConsumptionResponse>(validation.Error);
            }

            if (item.ProductId.HasValue)
            {
                meal.AddProduct(new ProductId(item.ProductId.Value), item.Amount);
            }
            else if (item.RecipeId.HasValue)
            {
                meal.AddRecipe(new RecipeId(item.RecipeId.Value), item.Amount);
            }
        }

        if (command.IsNutritionAutoCalculated)
        {
            var nutritionResult = await CalculateNutritionAsync(meal, command.UserId.Value, cancellationToken);
            if (nutritionResult.IsFailure)
            {
                return Result.Failure<ConsumptionResponse>(nutritionResult.Error);
            }

            meal.ApplyNutrition(
                nutritionResult.Value.Calories,
                nutritionResult.Value.Proteins,
                nutritionResult.Value.Fats,
                nutritionResult.Value.Carbs,
                nutritionResult.Value.Fiber,
                isAutoCalculated: true);
        }
        else
        {
            var manualNutritionResult = ManualNutritionValidator.Validate(
                command.ManualCalories,
                command.ManualProteins,
                command.ManualFats,
                command.ManualCarbs,
                command.ManualFiber);

            if (manualNutritionResult.IsFailure)
            {
                return Result.Failure<ConsumptionResponse>(manualNutritionResult.Error);
            }

            var manual = manualNutritionResult.Value;
            meal.ApplyNutrition(
                manual.Calories,
                manual.Proteins,
                manual.Fats,
                manual.Carbs,
                manual.Fiber,
                isAutoCalculated: false,
                manual.Calories,
                manual.Proteins,
                manual.Fats,
                manual.Carbs,
                manual.Fiber);
        }

        await mealRepository.UpdateAsync(meal, cancellationToken);

        var updated = await mealRepository.GetByIdAsync(
            meal.Id,
            command.UserId.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        if (updated is null)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Consumption.InvalidData("Failed to load updated consumption."));
        }

        return Result.Success(updated.ToResponse());
    }

    private static Result<MealType?> ParseMealType(string? mealType)
    {
        if (string.IsNullOrWhiteSpace(mealType))
        {
            return Result.Success<MealType?>(null);
        }

        return Enum.TryParse<MealType>(mealType, true, out var parsed)
            ? Result.Success<MealType?>(parsed)
            : Result.Failure<MealType?>(Errors.Validation.Invalid(nameof(mealType), "Unknown meal type value."));
    }

    private async Task<Result<MealNutritionSummary>> CalculateNutritionAsync(
        Domain.Entities.Meal meal,
        UserId userId,
        CancellationToken cancellationToken)
    {
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
        if (products.Count != productIds.Count)
        {
            var missingProduct = productIds.First(id => !products.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Product.NotAccessible(missingProduct.Value));
        }

        var recipes = await recipeRepository.GetByIdsAsync(recipeIds, userId, includePublic: true, cancellationToken);
        if (recipes.Count != recipeIds.Count)
        {
            var missingRecipe = recipeIds.First(id => !recipes.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Recipe.NotAccessible(missingRecipe.Value));
        }

        var summary = MealNutritionCalculator.Calculate(meal, products, recipes);
        return Result.Success(summary);
    }
}
