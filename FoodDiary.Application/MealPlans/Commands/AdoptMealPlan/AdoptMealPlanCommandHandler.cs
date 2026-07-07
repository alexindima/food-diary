using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.MealPlans;

namespace FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;

public sealed class AdoptMealPlanCommandHandler(IMealPlanWriteRepository mealPlanRepository)
    : ICommandHandler<AdoptMealPlanCommand, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(
        AdoptMealPlanCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<MealPlanModel>(userIdResult);
        }

        var planId = new MealPlanId(command.PlanId);
        MealPlan? sourcePlan = await mealPlanRepository.GetByIdAsync(planId, includeDays: true, cancellationToken).ConfigureAwait(false);
        if (sourcePlan is null) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotFound(command.PlanId));
        }

        if (!sourcePlan.IsCurated) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotCurated);
        }

        MealPlan adoptedPlan = sourcePlan.Adopt(userIdResult.Value);
        await mealPlanRepository.AddAsync(adoptedPlan, cancellationToken).ConfigureAwait(false);

        return Result.Success(adoptedPlan.ToModel());
    }
}
