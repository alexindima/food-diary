using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public class GetWeightEntriesQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetWeightEntriesQuery, Result<IReadOnlyList<WeightEntryModel>>> {
    public async Task<Result<IReadOnlyList<WeightEntryModel>>> Handle(
        GetWeightEntriesQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WeightEntryModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var normalizedFrom = query.DateFrom.HasValue ? (DateTime?)UtcDateNormalizer.NormalizeDateUsingLocalFallback(query.DateFrom.Value) : null;
        var normalizedTo = query.DateTo.HasValue ? (DateTime?)UtcDateNormalizer.NormalizeDateUsingLocalFallback(query.DateTo.Value) : null;

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
}
