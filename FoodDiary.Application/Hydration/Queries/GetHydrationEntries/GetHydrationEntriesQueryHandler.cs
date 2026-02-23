using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public class GetHydrationEntriesQueryHandler(IHydrationEntryRepository repository)
    : IQueryHandler<GetHydrationEntriesQuery, Result<IReadOnlyList<HydrationEntryResponse>>> {
    public async Task<Result<IReadOnlyList<HydrationEntryResponse>>> Handle(
        GetHydrationEntriesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<IReadOnlyList<HydrationEntryResponse>>(Errors.User.NotFound());
        }

        var dateUtc = NormalizeToUtcDate(query.DateUtc);
        var entries = await repository.GetByDateAsync(query.UserId.Value, dateUtc, cancellationToken);
        var response = entries.Select(e => e.ToResponse()).ToList();
        return Result.Success<IReadOnlyList<HydrationEntryResponse>>(response);
    }

    private static DateTime NormalizeToUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
