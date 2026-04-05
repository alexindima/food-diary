using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlanById;

public class GetMealPlanByIdQueryHandler(IMealPlanRepository mealPlanRepository)
    : IQueryHandler<GetMealPlanByIdQuery, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(
        GetMealPlanByIdQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<MealPlanModel>(userIdResult.Error);
        }

        var planId = new MealPlanId(query.PlanId);
        var plan = await mealPlanRepository.GetByIdAsync(planId, includeDays: true, cancellationToken);
        if (plan is null) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotFound(query.PlanId));
        }

        // Allow access to curated plans or user's own plans
        if (!plan.IsCurated && plan.UserId != userIdResult.Value) {
            return Result.Failure<MealPlanModel>(Errors.MealPlan.NotFound(query.PlanId));
        }

        return Result.Success(plan.ToModel());
    }
}
