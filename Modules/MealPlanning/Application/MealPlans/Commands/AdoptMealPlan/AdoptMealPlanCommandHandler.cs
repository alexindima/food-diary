using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.Users.Common;

using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.AdoptMealPlan;

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
        MealPlan? sourcePlan = await mealPlanRepository.GetCuratedByIdAsync(
            planId,
            includeDays: true,
            cancellationToken).ConfigureAwait(false);
        if (sourcePlan is null) {
            return Result.Failure<MealPlanModel>(MealPlanErrors.NotFound(command.PlanId));
        }

        MealPlan adoptedPlan = sourcePlan.Adopt(userIdResult.Value);
        await mealPlanRepository.AddAsync(adoptedPlan, cancellationToken).ConfigureAwait(false);

        return Result.Success(adoptedPlan.ToModel());
    }
}
