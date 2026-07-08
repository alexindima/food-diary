using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlanById;

public sealed class GetMealPlanByIdQueryHandler(IMealPlanReadService mealPlanReadService)
    : IQueryHandler<GetMealPlanByIdQuery, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(
        GetMealPlanByIdQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<MealPlanModel>(userIdResult);
        }

        Result<MealPlanId> planIdResult = RequiredIdParser.Parse(
            query.PlanId,
            nameof(query.PlanId),
            "Meal plan id must not be empty.",
            value => new MealPlanId(value));
        if (planIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<MealPlanModel, MealPlanId>(planIdResult);
        }

        MealPlanId planId = planIdResult.Value;
        MealPlanModel? plan = await mealPlanReadService
            .GetAccessibleByIdAsync(planId, userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotFound(query.PlanId));
        }

        return Result.Success(plan);
    }
}
