using FoodDiary.Modules.Hydration.Application.Mappings;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Hydration.Application.Abstractions.Models;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationEntries;

namespace FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationEntries;

public sealed class ReadHydrationEntriesQueryHandler(IHydrationEntryReadModelRepository hydrationEntryReadModelRepository) : IQueryHandler<ReadHydrationEntriesQuery, IReadOnlyList<HydrationEntryModel>> {
    public async Task<IReadOnlyList<HydrationEntryModel>> Handle(ReadHydrationEntriesQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<HydrationEntryReadModel> entries = await hydrationEntryReadModelRepository
            .GetByDateReadModelsAsync(request.UserId, request.DateUtc, cancellationToken)
            .ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }
}
