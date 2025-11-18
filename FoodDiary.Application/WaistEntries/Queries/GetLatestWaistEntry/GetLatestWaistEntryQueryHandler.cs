using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public class GetLatestWaistEntryQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetLatestWaistEntryQuery, Result<WaistEntryResponse?>>
{
    public async Task<Result<WaistEntryResponse?>> Handle(
        GetLatestWaistEntryQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<WaistEntryResponse?>(Errors.User.NotFound());
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
