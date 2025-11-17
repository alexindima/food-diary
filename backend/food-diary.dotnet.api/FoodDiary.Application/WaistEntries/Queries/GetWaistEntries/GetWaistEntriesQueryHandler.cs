using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;

public class GetWaistEntriesQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetWaistEntriesQuery, Result<IReadOnlyList<WaistEntryResponse>>>
{
    public async Task<Result<IReadOnlyList<WaistEntryResponse>>> Handle(
        GetWaistEntriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<IReadOnlyList<WaistEntryResponse>>(Errors.User.NotFound());
        }

        var entries = await waistEntryRepository.GetEntriesAsync(
            query.UserId.Value,
            query.DateFrom,
            query.DateTo,
            query.Limit,
            query.Descending,
            cancellationToken);

        var response = entries.Select(entry => entry.ToResponse()).ToList();
        return Result.Success<IReadOnlyList<WaistEntryResponse>>(response);
    }
}
