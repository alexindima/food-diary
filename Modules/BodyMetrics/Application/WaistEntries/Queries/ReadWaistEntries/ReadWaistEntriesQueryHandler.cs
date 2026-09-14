using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistEntries;

public sealed class ReadWaistEntriesQueryHandler(IWaistEntryReadModelRepository waistEntryRepository) : IRequestHandler<ReadWaistEntriesQuery, IReadOnlyList<WaistEntryModel>> {
    public async Task<IReadOnlyList<WaistEntryModel>> Handle(ReadWaistEntriesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime? dateFrom = request.DateFrom;
        DateTime? dateTo = request.DateTo;
        int? limit = request.Limit;
        bool descending = request.Descending;
        IReadOnlyList<WaistEntryReadModel> entries = await waistEntryRepository.GetEntryReadModelsAsync(
            userId,
            dateFrom,
            dateTo,
            limit,
            descending,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];

    }

}
