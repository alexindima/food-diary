using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableDailySummary;

internal sealed class GetWearableDailySummaryQueryHandler(
    IWearableSyncReadModelRepository syncRepository,
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

        IReadOnlyList<WearableSyncEntryReadModel> entries = await syncRepository
            .GetDailySummaryReadModelsAsync(userIdResult.Value, query.Date, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(WearableSummaryCalculator.Calculate(query.Date, entries.Select(entry => (entry.DataType, entry.Value))));
    }
}
