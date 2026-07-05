using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;

public sealed class GetFastingTelemetrySummaryQueryHandler(IFastingTelemetrySummaryReadService readService)
    : IQueryHandler<GetFastingTelemetrySummaryQuery, Result<FastingTelemetrySummaryModel>> {
    public async Task<Result<FastingTelemetrySummaryModel>> Handle(
        GetFastingTelemetrySummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetAsync(query.Hours, cancellationToken).ConfigureAwait(false);
    }
}
