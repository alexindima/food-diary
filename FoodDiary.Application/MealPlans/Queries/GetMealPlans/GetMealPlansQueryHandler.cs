using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryHandler(IMealPlanReadService mealPlanReadService)
    : IQueryHandler<GetMealPlansQuery, Result<IReadOnlyList<MealPlanSummaryModel>>> {
    public async Task<Result<IReadOnlyList<MealPlanSummaryModel>>> Handle(
        GetMealPlansQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<MealPlanSummaryModel>>(userIdResult);
        }

        DietType? dietTypeFilter = EnumFilterParser.ParseOptional<DietType>(query.DietType);

        IReadOnlyList<MealPlanSummaryModel> all = await mealPlanReadService
            .GetAllAsync(userIdResult.Value, dietTypeFilter, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(all);
    }
}
