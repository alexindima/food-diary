using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public class GetLatestWaistEntryQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetLatestWaistEntryQuery, Result<WaistEntryResponse?>> {
    public async Task<Result<WaistEntryResponse?>> Handle(
        GetLatestWaistEntryQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<WaistEntryResponse?>(Errors.Authentication.InvalidToken);
        }

        var entries = await waistEntryRepository.GetEntriesAsync(
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
