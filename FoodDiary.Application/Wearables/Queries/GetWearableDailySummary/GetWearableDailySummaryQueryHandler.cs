using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;

public sealed class GetWearableDailySummaryQueryHandler(IWearableReadService wearableReadService)
    : IQueryHandler<GetWearableDailySummaryQuery, Result<WearableDailySummaryModel>> {
    public async Task<Result<WearableDailySummaryModel>> Handle(
        GetWearableDailySummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<WearableDailySummaryModel>(userIdResult);
        }

        WearableDailySummaryModel summary = await wearableReadService
            .GetDailySummaryAsync(userIdResult.Value, query.Date, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(summary);
    }
}
