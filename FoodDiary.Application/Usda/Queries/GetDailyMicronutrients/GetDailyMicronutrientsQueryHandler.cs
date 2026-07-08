using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;

public sealed class GetDailyMicronutrientsQueryHandler(
    IUsdaDailyMicronutrientReadService dailyMicronutrientReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDailyMicronutrientsQuery, Result<DailyMicronutrientSummaryModel>> {
    public async Task<Result<DailyMicronutrientSummaryModel>> Handle(
        GetDailyMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DailyMicronutrientSummaryModel>(userIdResult);
        }

        DailyMicronutrientSummaryModel summary = await dailyMicronutrientReadService.GetDailySummaryAsync(
            userIdResult.Value,
            query.Date,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(summary);
    }
}
