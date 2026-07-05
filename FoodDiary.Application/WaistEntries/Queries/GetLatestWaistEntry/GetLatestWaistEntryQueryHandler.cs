using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
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
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<WaistEntryModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WaistEntryModel?>(accessError);
        }

        WaistEntryModel? latest = await waistEntryReadService.GetLatestAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(latest);
    }
}
