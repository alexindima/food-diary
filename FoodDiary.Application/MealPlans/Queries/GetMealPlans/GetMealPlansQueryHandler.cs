using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryHandler(
    IMealPlanReadService mealPlanReadService,
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

        IReadOnlyList<MealPlanSummaryModel> all = await mealPlanReadService
            .GetAllAsync(userIdResult.Value, dietTypeFilter, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(all);
    }
}
