using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public class GetLatestWaistEntryQueryHandler(
    IWaistEntryRepository waistEntryRepository,
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

        IReadOnlyList<WaistEntry> entries = await waistEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 1,
            descending: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        WaistEntry? latest = entries.Count > 0 ? entries[0] : null;
        return Result.Success(latest?.ToModel());
    }
}
