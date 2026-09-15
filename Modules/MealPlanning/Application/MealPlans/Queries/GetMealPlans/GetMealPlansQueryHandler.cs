using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryHandler(
    IMealPlanReadModelRepository mealPlanRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMealPlansQuery, Result<IReadOnlyList<MealPlanSummaryModel>>> {
    public async Task<Result<IReadOnlyList<MealPlanSummaryModel>>> Handle(
        GetMealPlansQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<MealPlanSummaryModel>>(userIdResult);
        }

        DietType? dietTypeFilter = EnumFilterParser.ParseOptional<DietType>(query.DietType);

        IReadOnlyList<MealPlanSummaryModel> all = await GetAllAsync(userIdResult.Value, dietTypeFilter, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(all);
    }
    private async Task<IReadOnlyList<MealPlanSummaryModel>> GetAllAsync(
        UserId userId,
        DietType? dietTypeFilter,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealPlanSummaryReadModel> curatedPlans = await mealPlanRepository
            .GetCuratedSummaryReadModelsAsync(dietTypeFilter, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<MealPlanSummaryReadModel> userPlans = await mealPlanRepository
            .GetByUserSummaryReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return curatedPlans
            .Concat(userPlans)
            .Select(plan => plan.ToSummaryModel())
            .ToList();
    }
}
