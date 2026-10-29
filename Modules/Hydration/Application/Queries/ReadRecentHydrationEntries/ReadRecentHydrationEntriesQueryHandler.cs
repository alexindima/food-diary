using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Hydration.Application.Abstractions.Models;
using FoodDiary.Modules.Hydration.Application.Mappings;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadRecentHydrationEntries;

namespace FoodDiary.Modules.Hydration.Application.Queries.ReadRecentHydrationEntries;

public sealed class ReadRecentHydrationEntriesQueryHandler(IHydrationEntryReadModelRepository repository) : IQueryHandler<ReadRecentHydrationEntriesQuery, IReadOnlyList<HydrationEntryModel>> {
    public async Task<IReadOnlyList<HydrationEntryModel>> Handle(ReadRecentHydrationEntriesQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<HydrationEntryReadModel> entries = await repository.GetRecentReadModelsAsync(
            request.UserId, request.StartUtc, request.EndUtc, Math.Clamp(request.Limit, 1, 100), cancellationToken).ConfigureAwait(false);
        return [.. entries.Select(entry => entry.ToModel())];
    }
}
