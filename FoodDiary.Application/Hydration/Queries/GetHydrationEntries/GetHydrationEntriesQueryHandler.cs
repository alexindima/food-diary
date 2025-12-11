using System.Linq;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Contracts.Hydration;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public class GetHydrationEntriesQueryHandler(IHydrationEntryRepository repository)
    : IQueryHandler<GetHydrationEntriesQuery, Result<IReadOnlyList<HydrationEntryResponse>>>
{
    public async Task<Result<IReadOnlyList<HydrationEntryResponse>>> Handle(
        GetHydrationEntriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<IReadOnlyList<HydrationEntryResponse>>(Errors.User.NotFound());
        }

        var entries = await repository.GetByDateAsync(query.UserId.Value, query.DateUtc, cancellationToken);
        var response = entries.Select(e => e.ToResponse()).ToList();
        return Result.Success<IReadOnlyList<HydrationEntryResponse>>(response);
    }
}
