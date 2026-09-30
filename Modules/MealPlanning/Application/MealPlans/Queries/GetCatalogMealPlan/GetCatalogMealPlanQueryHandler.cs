using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;

public sealed class GetCatalogMealPlanQueryHandler(IMealPlanCompositionReader reader)
    : IQueryHandler<GetCatalogMealPlanQuery, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(GetCatalogMealPlanQuery query, CancellationToken cancellationToken) {
        Result<MealPlanId> id = RequiredIdParser.Parse(query.Id, nameof(query.Id), "Meal plan id is required.", value => new MealPlanId(value));
        if (id.IsFailure) { return Result.Failure<MealPlanModel>(id.Error); }
        FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanReadModel? plan = await reader.GetReadModelByIdAsync(id.Value, cancellationToken).ConfigureAwait(false);
        return plan?.UserId.HasValue != false
            ? Result.Failure<MealPlanModel>(MealPlanErrors.NotFound(query.Id)) : Result.Success(plan.ToModel());
    }
}
