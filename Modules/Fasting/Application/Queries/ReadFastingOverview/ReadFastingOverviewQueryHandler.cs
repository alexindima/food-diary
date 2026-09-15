using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Fasting.Application.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingOverview;
using FoodDiary.Modules.Fasting.Application.Services;

namespace FoodDiary.Modules.Fasting.Application.Queries.ReadFastingOverview;

public sealed class ReadFastingOverviewQueryHandler(IFastingOccurrenceReadModelRepository fastingOccurrenceRepository,
    IFastingCheckInReadModelRepository fastingCheckInRepository,
    IFastingAnalyticsService fastingAnalyticsService,
    TimeProvider dateTimeProvider) : IQueryHandler<ReadFastingOverviewQuery, FastingOverviewModel> {
    public async Task<FastingOverviewModel> Handle(ReadFastingOverviewQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        FastingOccurrenceReadModel? current = await GetCurrentOccurrenceAsync(userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingCheckInReadModel> currentCheckIns = current is null
            ? []
            : await GetCheckInsAsync(current, cancellationToken).ConfigureAwait(false);
        FastingStatsModel stats = await fastingAnalyticsService.GetStatsAsync(userId, now, cancellationToken).ConfigureAwait(false);
        FastingInsightsModel insights = await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken).ConfigureAwait(false);
        (DateTime fromUtc, DateTime toUtc) = fastingAnalyticsService.GetDefaultHistoryWindow(now);
        PagedResponse<FastingSessionModel> history = await fastingAnalyticsService.GetHistoryAsync(
            userId,
            1,
            OverviewHistoryPageSize,
            fromUtc,
            toUtc,
            cancellationToken).ConfigureAwait(false);

        return new FastingOverviewModel(
            current?.ToModel(current.Plan, currentCheckIns),
            stats,
            insights,
            history);
    }

    private const int OverviewHistoryPageSize = 10;

    private Task<FastingOccurrenceReadModel?> GetCurrentOccurrenceAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        fastingOccurrenceRepository.GetCurrentReadModelAsync(userId, cancellationToken);

    private Task<IReadOnlyList<FastingCheckInReadModel>> GetCheckInsAsync(
        FastingOccurrenceReadModel occurrence,
        CancellationToken cancellationToken) =>
        fastingCheckInRepository.GetByOccurrenceIdReadModelsAsync([occurrence.Id], cancellationToken);

}
