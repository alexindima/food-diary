using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Models;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryHandler(
    IMealPlanReadModelRepository mealPlanRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMealPlansQuery, Result<PagedResponse<MealPlanSummaryModel>>> {
    public async Task<Result<PagedResponse<MealPlanSummaryModel>>> Handle(
        GetMealPlansQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<MealPlanSummaryModel>>(userIdResult);
        }

        DietType? dietTypeFilter = EnumFilterParser.ParseOptional<DietType>(query.DietType);

        (IReadOnlyList<MealPlanSummaryReadModel> items, int total) = await mealPlanRepository
            .GetPageSummaryReadModelsAsync(userIdResult.Value, dietTypeFilter, query.Page, query.Limit, cancellationToken)
            .ConfigureAwait(false);
        int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.Limit);
        return Result.Success(new PagedResponse<MealPlanSummaryModel>(
            items.Select(plan => plan.ToSummaryModel()).ToArray(), query.Page, query.Limit, totalPages, total));
    }
}
