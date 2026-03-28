using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public class GetHydrationEntriesQueryHandler(IHydrationEntryRepository repository)
    : IQueryHandler<GetHydrationEntriesQuery, Result<IReadOnlyList<HydrationEntryModel>>> {
    public async Task<Result<IReadOnlyList<HydrationEntryModel>>> Handle(
        GetHydrationEntriesQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<HydrationEntryModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;

        var dateUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateUtc);
        var entries = await repository.GetByDateAsync(userId, dateUtc, cancellationToken);
        var response = entries.Select(e => e.ToModel()).ToList();
        return Result.Success<IReadOnlyList<HydrationEntryModel>>(response);
    }
}
