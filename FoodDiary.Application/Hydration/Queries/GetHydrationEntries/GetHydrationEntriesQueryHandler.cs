using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public class GetHydrationEntriesQueryHandler(IHydrationEntryRepository repository)
    : IQueryHandler<GetHydrationEntriesQuery, Result<IReadOnlyList<HydrationEntryModel>>> {
    public async Task<Result<IReadOnlyList<HydrationEntryModel>>> Handle(
        GetHydrationEntriesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<HydrationEntryModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);

        var dateUtc = NormalizeToUtcDate(query.DateUtc);
        var entries = await repository.GetByDateAsync(userId, dateUtc, cancellationToken);
        var response = entries.Select(e => e.ToModel()).ToList();
        return Result.Success<IReadOnlyList<HydrationEntryModel>>(response);
    }

    private static DateTime NormalizeToUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
