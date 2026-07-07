using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public sealed class GetLatestWaistEntryQueryHandler(
    IWaistEntryReadService waistEntryReadService,
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
        WaistEntryModel? latest = await waistEntryReadService.GetLatestAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(latest);
    }
}
