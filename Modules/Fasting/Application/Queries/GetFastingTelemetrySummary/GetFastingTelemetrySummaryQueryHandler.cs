using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingTelemetrySummary;

public sealed class GetFastingTelemetrySummaryQueryHandler(IFastingTelemetrySummaryReadService readService)
    : IQueryHandler<GetFastingTelemetrySummaryQuery, Result<FastingTelemetrySummaryModel>> {
    public async Task<Result<FastingTelemetrySummaryModel>> Handle(
        GetFastingTelemetrySummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetAsync(query.Hours, cancellationToken).ConfigureAwait(false);
    }
}
