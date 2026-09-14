using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetLatestWeightEntry;

public sealed class GetLatestWeightEntryQueryHandler(
    ISender weightEntryReadService,
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
        WeightEntryModel? latest = await weightEntryReadService.Send(new ReadLatestWeightEntryQuery(UserId: userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(latest);
    }
}
