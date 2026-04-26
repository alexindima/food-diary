using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
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
    public async Task<Result<ConsumptionModel>> Handle(
        RepeatMealCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ConsumptionModel>(accessError);
        }

        var sourceMealId = new MealId(command.MealId);
        var sourceMeal = await mealRepository.GetByIdAsync(sourceMealId, userId, includeItems: true, cancellationToken: cancellationToken);

        if (sourceMeal is null) {
            return Result.Failure<ConsumptionModel>(Errors.Consumption.NotFound(command.MealId));
        }

        var mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType ?? sourceMeal.MealType?.ToString(),
            "MealType",
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(mealTypeResult.Error);
        }

        var newMeal = Meal.Create(userId, command.TargetDate, mealTypeResult.Value);

        foreach (var item in sourceMeal.Items) {
            if (item.ProductId.HasValue) {
                newMeal.AddProduct(item.ProductId.Value, item.Amount);
            } else if (item.RecipeId.HasValue) {
                newMeal.AddRecipe(item.RecipeId.Value, item.Amount);
            }
        }

        var nutritionResult = await mealNutritionService.CalculateAsync(newMeal, userId, cancellationToken);
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

        await mealRepository.AddAsync(newMeal, cancellationToken);

        return Result.Success(newMeal.ToModel());
    }
}
