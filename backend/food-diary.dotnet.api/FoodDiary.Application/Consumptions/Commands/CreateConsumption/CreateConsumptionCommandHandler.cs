using System;
using System.Collections.Generic;
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
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public class CreateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IProductRepository productRepository,
    IRecipeRepository recipeRepository)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionResponse>>
{
    public async Task<Result<ConsumptionResponse>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId is null || command.UserId == UserId.Empty)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Authentication.InvalidToken);
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

        var meal = Meal.Create(command.UserId.Value, command.Date, mealTypeResult.Value, command.Comment);

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
            nutritionResult.Value.Fiber);

        await mealRepository.AddAsync(meal, cancellationToken);

        var created = await mealRepository.GetByIdAsync(
            meal.Id,
            command.UserId.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        if (created is null)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Consumption.InvalidData("Failed to load created consumption."));
        }

        return Result.Success(created.ToResponse());
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
        Meal meal,
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
