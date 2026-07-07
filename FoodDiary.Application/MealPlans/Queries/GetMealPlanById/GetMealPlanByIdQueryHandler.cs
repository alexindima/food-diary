using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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

        var planId = new MealPlanId(query.PlanId);
        MealPlanModel? plan = await mealPlanReadService
            .GetAccessibleByIdAsync(planId, userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotFound(query.PlanId));
        }

        return Result.Success(plan);
    }
}
