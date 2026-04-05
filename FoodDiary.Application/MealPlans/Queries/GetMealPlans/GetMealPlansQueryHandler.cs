using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlans;

public class GetMealPlansQueryHandler(IMealPlanRepository mealPlanRepository)
    : IQueryHandler<GetMealPlansQuery, Result<IReadOnlyList<MealPlanSummaryModel>>> {
    public async Task<Result<IReadOnlyList<MealPlanSummaryModel>>> Handle(
        GetMealPlansQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<MealPlanSummaryModel>>(userIdResult.Error);
        }

        DietType? dietTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(query.DietType) && Enum.TryParse<DietType>(query.DietType, true, out var parsed)) {
            dietTypeFilter = parsed;
        }

        var curatedPlans = await mealPlanRepository.GetCuratedAsync(dietTypeFilter, cancellationToken);
        var userPlans = await mealPlanRepository.GetByUserAsync(userIdResult.Value, cancellationToken);

        var all = curatedPlans.Concat(userPlans)
            .Select(p => p.ToSummaryModel())
            .ToList();

        return Result.Success<IReadOnlyList<MealPlanSummaryModel>>(all);
    }
}
