using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadLatestWaistEntry;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadLatestWaistEntry;

public sealed class ReadLatestWaistEntryQueryHandler(IWaistEntryReadModelRepository repository)
    : IRequestHandler<ReadLatestWaistEntryQuery, WaistEntryModel?> {
    public async Task<WaistEntryModel?> Handle(ReadLatestWaistEntryQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<WaistEntryReadModel> entries = await repository.GetEntryReadModelsAsync(
            request.UserId, dateFrom: null, dateTo: null, limit: 1, descending: true, cancellationToken).ConfigureAwait(false);
        return entries.Count > 0 ? entries[0].ToModel() : null;
    }
}
