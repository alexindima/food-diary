using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.MealPlans;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryHandler(IMealPlanReadRepository mealPlanRepository)
    : IQueryHandler<GetMealPlansQuery, Result<IReadOnlyList<MealPlanSummaryModel>>> {
    public async Task<Result<IReadOnlyList<MealPlanSummaryModel>>> Handle(
        GetMealPlansQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<MealPlanSummaryModel>>(userIdResult.Error);
        }

        DietType? dietTypeFilter = EnumFilterParser.ParseOptional<DietType>(query.DietType);

        IReadOnlyList<MealPlan> curatedPlans = await mealPlanRepository.GetCuratedAsync(dietTypeFilter, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MealPlan> userPlans = await mealPlanRepository.GetByUserAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);

        var all = curatedPlans.Concat(userPlans)
            .Select(p => p.ToSummaryModel())
            .ToList();

        return Result.Success<IReadOnlyList<MealPlanSummaryModel>>(all);
    }
}
