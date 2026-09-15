using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlanById;

public sealed class GetMealPlanByIdQueryHandler(
    IMealPlanReadModelRepository mealPlanRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMealPlanByIdQuery, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(
        GetMealPlanByIdQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<MealPlanModel>(userIdResult);
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
        MealPlanModel? plan = await GetAccessibleByIdAsync(planId, userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<MealPlanModel>(MealPlanErrors.NotFound(query.PlanId));
        }

        return Result.Success(plan);
    }
    private async Task<MealPlanModel?> GetAccessibleByIdAsync(
        MealPlanId mealPlanId,
        UserId userId,
        CancellationToken cancellationToken) {
        MealPlanReadModel? plan = await mealPlanRepository
            .GetReadModelByIdAsync(mealPlanId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null || (!plan.IsCurated && plan.UserId != userId.Value)) {
            return null;
        }

        return plan.ToModel();
    }
}
