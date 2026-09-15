using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistEntries;

public sealed class GetWaistEntriesQueryHandler(
    ISender sender,
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

        IReadOnlyList<WaistEntryModel> response = await sender.Send(new ReadWaistEntriesQuery(UserId: userId, DateFrom: normalizedFrom, DateTo: normalizedTo, Limit: PaginationPolicy.NormalizeCollectionLimit(query.Limit), Descending: query.Descending), cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WaistEntryModel>>(response);
    }
}
