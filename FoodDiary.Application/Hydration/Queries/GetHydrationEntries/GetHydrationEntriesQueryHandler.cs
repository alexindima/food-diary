using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public sealed class GetHydrationEntriesQueryHandler(
    IHydrationEntryReadService hydrationEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetHydrationEntriesQuery, Result<IReadOnlyList<HydrationEntryModel>>> {
    public async Task<Result<IReadOnlyList<HydrationEntryModel>>> Handle(
        GetHydrationEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<HydrationEntryModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<HydrationEntryModel>>(accessError);
        }

        DateTime dateUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateUtc);
        IReadOnlyList<HydrationEntryModel> response = await hydrationEntryReadService
            .GetEntriesByDateAsync(userId, dateUtc, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<HydrationEntryModel>>(response);
    }
}
