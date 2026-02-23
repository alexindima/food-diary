using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public class GetWeightEntriesQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetWeightEntriesQuery, Result<IReadOnlyList<WeightEntryResponse>>> {
    public async Task<Result<IReadOnlyList<WeightEntryResponse>>> Handle(
        GetWeightEntriesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<IReadOnlyList<WeightEntryResponse>>(Errors.Authentication.InvalidToken);
        }

        var normalizedFrom = query.DateFrom.HasValue ? (DateTime?)NormalizeUtcDate(query.DateFrom.Value) : null;
        var normalizedTo = query.DateTo.HasValue ? (DateTime?)NormalizeUtcDate(query.DateTo.Value) : null;

        var entries = await weightEntryRepository.GetEntriesAsync(
            query.UserId.Value,
            normalizedFrom,
            normalizedTo,
            query.Limit,
            query.Descending,
            cancellationToken);

        var response = entries.Select(entry => entry.ToResponse()).ToList();
        return Result.Success<IReadOnlyList<WeightEntryResponse>>(response);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
