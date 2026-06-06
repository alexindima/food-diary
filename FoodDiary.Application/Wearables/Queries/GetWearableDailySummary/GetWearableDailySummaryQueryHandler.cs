using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Wearables;

namespace FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;

public class GetWearableDailySummaryQueryHandler(IWearableSyncRepository syncRepository)
    : IQueryHandler<GetWearableDailySummaryQuery, Result<WearableDailySummaryModel>> {
    public async Task<Result<WearableDailySummaryModel>> Handle(
        GetWearableDailySummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WearableDailySummaryModel>(userIdResult.Error);
        }

        IReadOnlyList<WearableSyncEntry> entries = await syncRepository.GetDailySummaryAsync(userIdResult.Value, query.Date, cancellationToken).ConfigureAwait(false);
        WearableDailySummaryModel summary = SyncWearableDataCommandHandler.MapToSummary(query.Date, entries);

        return Result.Success(summary);
    }
}
