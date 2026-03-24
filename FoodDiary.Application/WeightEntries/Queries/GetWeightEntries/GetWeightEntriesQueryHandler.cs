using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public class GetWeightEntriesQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetWeightEntriesQuery, Result<IReadOnlyList<WeightEntryModel>>> {
    public async Task<Result<IReadOnlyList<WeightEntryModel>>> Handle(
        GetWeightEntriesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == Guid.Empty) {
            return Result.Failure<IReadOnlyList<WeightEntryModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var normalizedFrom = query.DateFrom.HasValue ? (DateTime?)NormalizeUtcDate(query.DateFrom.Value) : null;
        var normalizedTo = query.DateTo.HasValue ? (DateTime?)NormalizeUtcDate(query.DateTo.Value) : null;

        var entries = await weightEntryRepository.GetEntriesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.Limit,
            query.Descending,
            cancellationToken);

        var response = entries.Select(entry => entry.ToModel()).ToList();
        return Result.Success<IReadOnlyList<WeightEntryModel>>(response);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
