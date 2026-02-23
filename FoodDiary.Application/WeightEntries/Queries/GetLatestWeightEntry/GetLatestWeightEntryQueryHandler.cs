using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public class GetLatestWeightEntryQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetLatestWeightEntryQuery, Result<WeightEntryResponse?>> {
    public async Task<Result<WeightEntryResponse?>> Handle(
        GetLatestWeightEntryQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<WeightEntryResponse?>(Errors.Authentication.InvalidToken);
        }

        var entries = await weightEntryRepository.GetEntriesAsync(
            query.UserId.Value,
            dateFrom: null,
            dateTo: null,
            limit: 1,
            descending: true,
            cancellationToken: cancellationToken);

        var latest = entries.FirstOrDefault();
        return Result.Success(latest?.ToResponse());
    }
}
