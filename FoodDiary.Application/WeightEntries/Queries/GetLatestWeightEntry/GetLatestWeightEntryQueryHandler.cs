using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public sealed class GetLatestWeightEntryQueryHandler(
    IWeightEntryReadService weightEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetLatestWeightEntryQuery, Result<WeightEntryModel?>> {
    public async Task<Result<WeightEntryModel?>> Handle(
        GetLatestWeightEntryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WeightEntryModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WeightEntryModel? latest = await weightEntryReadService.GetLatestAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(latest);
    }
}
