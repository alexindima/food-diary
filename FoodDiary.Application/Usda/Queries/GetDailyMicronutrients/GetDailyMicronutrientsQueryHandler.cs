using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;

public sealed class GetDailyMicronutrientsQueryHandler(IUsdaDailyMicronutrientReadService dailyMicronutrientReadService)
    : IQueryHandler<GetDailyMicronutrientsQuery, Result<DailyMicronutrientSummaryModel>> {
    public async Task<Result<DailyMicronutrientSummaryModel>> Handle(
        GetDailyMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<DailyMicronutrientSummaryModel>(userIdResult.Error);
        }

        DailyMicronutrientSummaryModel summary = await dailyMicronutrientReadService.GetDailySummaryAsync(
            userIdResult.Value,
            query.Date,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(summary);
    }
}
