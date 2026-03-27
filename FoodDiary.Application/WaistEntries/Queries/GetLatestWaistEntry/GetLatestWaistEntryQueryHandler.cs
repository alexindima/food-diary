using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public class GetLatestWaistEntryQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetLatestWaistEntryQuery, Result<WaistEntryModel?>> {
    public async Task<Result<WaistEntryModel?>> Handle(
        GetLatestWaistEntryQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == Guid.Empty) {
            return Result.Failure<WaistEntryModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var entries = await waistEntryRepository.GetEntriesAsync(
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
