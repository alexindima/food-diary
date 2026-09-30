using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlans;

public sealed class GetCatalogMealPlansQueryHandler(IMealPlanCatalogRepository repository)
    : IQueryHandler<GetCatalogMealPlansQuery, Result<IReadOnlyList<MealPlanSummaryModel>>> {
    public async Task<Result<IReadOnlyList<MealPlanSummaryModel>>> Handle(GetCatalogMealPlansQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanSummaryReadModel> items = await repository.GetPageAsync(query.Page, query.Limit, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<MealPlanSummaryModel>>([.. items.Select(item => item.ToSummaryModel())]);
    }
}
