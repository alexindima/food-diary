using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingInsights;
using FoodDiary.Modules.Fasting.Application.Services;

namespace FoodDiary.Modules.Fasting.Application.Queries.ReadFastingInsights;

public sealed class ReadFastingInsightsQueryHandler(IFastingOccurrenceReadModelRepository fastingOccurrenceRepository,
    IFastingAnalyticsService fastingAnalyticsService,
    TimeProvider dateTimeProvider) : IQueryHandler<ReadFastingInsightsQuery, FastingInsightsModel> {
    public async Task<FastingInsightsModel> Handle(ReadFastingInsightsQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        FastingOccurrenceReadModel? current = await GetCurrentOccurrenceAsync(userId, cancellationToken).ConfigureAwait(false);
        return await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken).ConfigureAwait(false);
    }

    private Task<FastingOccurrenceReadModel?> GetCurrentOccurrenceAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        fastingOccurrenceRepository.GetCurrentReadModelAsync(userId, cancellationToken);

}
