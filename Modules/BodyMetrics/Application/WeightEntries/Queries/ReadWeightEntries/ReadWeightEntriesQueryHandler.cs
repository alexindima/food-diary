using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightEntries;

public sealed class ReadWeightEntriesQueryHandler(IWeightEntryReadModelRepository weightEntryRepository) : IRequestHandler<ReadWeightEntriesQuery, IReadOnlyList<WeightEntryModel>> {
    public async Task<IReadOnlyList<WeightEntryModel>> Handle(ReadWeightEntriesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime? dateFrom = request.DateFrom;
        DateTime? dateTo = request.DateTo;
        int? limit = request.Limit;
        bool descending = request.Descending;
        IReadOnlyList<WeightEntryReadModel> entries = await weightEntryRepository.GetEntryReadModelsAsync(
            userId,
            dateFrom,
            dateTo,
            limit,
            descending,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];

    }

}
