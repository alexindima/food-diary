using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;

public class AdoptMealPlanCommandHandler(IMealPlanRepository mealPlanRepository)
    : ICommandHandler<AdoptMealPlanCommand, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(
        AdoptMealPlanCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<MealPlanModel>(userIdResult.Error);
        }

        var planId = new MealPlanId(command.PlanId);
        var sourcePlan = await mealPlanRepository.GetByIdAsync(planId, includeDays: true, cancellationToken);
        if (sourcePlan is null) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotFound(command.PlanId));
        }

        if (!sourcePlan.IsCurated) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotCurated);
        }

        var adoptedPlan = sourcePlan.Adopt(userIdResult.Value);
        await mealPlanRepository.AddAsync(adoptedPlan, cancellationToken);

        // Re-fetch with includes for full model
        var saved = await mealPlanRepository.GetByIdAsync(adoptedPlan.Id, includeDays: true, cancellationToken);
        return Result.Success(saved!.ToModel());
    }
}
