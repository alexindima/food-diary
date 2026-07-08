using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;

public sealed class GetWearableDailySummaryQueryHandler(
    IWearableReadService wearableReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWearableDailySummaryQuery, Result<WearableDailySummaryModel>> {
    public async Task<Result<WearableDailySummaryModel>> Handle(
        GetWearableDailySummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WearableDailySummaryModel>(userIdResult);
        }

        WearableDailySummaryModel summary = await wearableReadService
            .GetDailySummaryAsync(userIdResult.Value, query.Date, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(summary);
    }
}
