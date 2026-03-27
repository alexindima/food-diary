using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public class GetLatestWeightEntryQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetLatestWeightEntryQuery, Result<WeightEntryModel?>> {
    public async Task<Result<WeightEntryModel?>> Handle(
        GetLatestWeightEntryQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == Guid.Empty) {
            return Result.Failure<WeightEntryModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var entries = await weightEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 1,
            descending: true,
            cancellationToken: cancellationToken);

        var latest = entries.FirstOrDefault();
        return Result.Success(latest?.ToModel());
    }
}
