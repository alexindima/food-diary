using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Contracts.WeightEntries;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public class GetWeightEntriesQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetWeightEntriesQuery, Result<IReadOnlyList<WeightEntryResponse>>>
{
    public async Task<Result<IReadOnlyList<WeightEntryResponse>>> Handle(
        GetWeightEntriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<IReadOnlyList<WeightEntryResponse>>(Errors.User.NotFound());
        }

        var entries = await weightEntryRepository.GetEntriesAsync(
            query.UserId.Value,
            query.DateFrom?.Date,
            query.DateTo?.Date,
            query.Limit,
            query.Descending,
            cancellationToken);

        var response = entries.Select(e => e.ToResponse()).ToList();
        return Result.Success<IReadOnlyList<WeightEntryResponse>>(response);
    }
}
