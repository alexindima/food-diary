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

        var shouldCopyMealImage = !sourceMeal.AiSessions.Any(session => session.ImageAssetId.HasValue);
        var newMeal = Meal.Create(
            userId,
            command.TargetDate,
            mealTypeResult.Value,
            sourceMeal.Comment,
            shouldCopyMealImage ? sourceMeal.ImageUrl : null,
            shouldCopyMealImage ? sourceMeal.ImageAssetId : null,
            sourceMeal.PreMealSatietyLevel,
            sourceMeal.PostMealSatietyLevel);

        foreach (var item in sourceMeal.Items) {
            if (item.ProductId.HasValue) {
                newMeal.AddProduct(item.ProductId.Value, item.Amount);
            } else if (item.RecipeId.HasValue) {
                newMeal.AddRecipe(item.RecipeId.Value, item.Amount);
            }
        }

        var targetRecognizedAtUtc = command.TargetDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(command.TargetDate, DateTimeKind.Utc)
            : command.TargetDate.ToUniversalTime();

        foreach (var session in sourceMeal.AiSessions) {
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
                    item.Alcohol))
                .ToList();

            newMeal.AddAiSession(session.ImageAssetId, session.Source, targetRecognizedAtUtc, session.Notes, items);
        }

        if (sourceMeal.IsNutritionAutoCalculated) {
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
        } else {
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

        await mealRepository.AddAsync(newMeal, cancellationToken);

        return Result.Success(newMeal.ToModel());
    }
}
