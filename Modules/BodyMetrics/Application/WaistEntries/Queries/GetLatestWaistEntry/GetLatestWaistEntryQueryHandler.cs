using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadLatestWaistEntry;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetLatestWaistEntry;

public sealed class GetLatestWaistEntryQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetLatestWaistEntryQuery, Result<WaistEntryModel?>> {
    public async Task<Result<WaistEntryModel?>> Handle(
        GetLatestWaistEntryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WaistEntryModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WaistEntryModel? latest = await sender.Send(new ReadLatestWaistEntryQuery(UserId: userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(latest);
    }
}
