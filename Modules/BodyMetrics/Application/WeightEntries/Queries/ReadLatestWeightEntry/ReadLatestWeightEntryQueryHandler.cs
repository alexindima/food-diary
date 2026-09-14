using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadLatestWeightEntry;

public sealed class ReadLatestWeightEntryQueryHandler(IWeightEntryReadModelRepository repository)
    : IRequestHandler<ReadLatestWeightEntryQuery, WeightEntryModel?> {
    public async Task<WeightEntryModel?> Handle(ReadLatestWeightEntryQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<WeightEntryReadModel> entries = await repository.GetEntryReadModelsAsync(
            request.UserId, dateFrom: null, dateTo: null, limit: 1, descending: true, cancellationToken).ConfigureAwait(false);
        return entries.Count > 0 ? entries[0].ToModel() : null;
    }
}
