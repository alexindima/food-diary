using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.MealPlans;

namespace FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;

public sealed class AdoptMealPlanCommandHandler(
    IMealPlanWriteRepository mealPlanRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<AdoptMealPlanCommand, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(
        AdoptMealPlanCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<MealPlanModel>(userIdResult);
        }

        Result<MealPlanId> planIdResult = RequiredIdParser.Parse(
            command.PlanId,
            nameof(command.PlanId),
            "Meal plan id must not be empty.",
            value => new MealPlanId(value));
        if (planIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<MealPlanModel, MealPlanId>(planIdResult);
        }

        MealPlanId planId = planIdResult.Value;
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
