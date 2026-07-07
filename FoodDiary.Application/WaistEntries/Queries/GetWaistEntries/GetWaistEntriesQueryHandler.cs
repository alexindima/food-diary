using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;

public sealed class GetWaistEntriesQueryHandler(
    IWaistEntryReadService waistEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWaistEntriesQuery, Result<IReadOnlyList<WaistEntryModel>>> {
    public async Task<Result<IReadOnlyList<WaistEntryModel>>> Handle(
        GetWaistEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WaistEntryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime? normalizedFrom = query.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom.Value)
            : null;
        DateTime? normalizedTo = query.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo.Value)
            : null;

        IReadOnlyList<WaistEntryModel> response = await waistEntryReadService.GetEntriesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.Limit,
            query.Descending,
            cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WaistEntryModel>>(response);
    }
}
