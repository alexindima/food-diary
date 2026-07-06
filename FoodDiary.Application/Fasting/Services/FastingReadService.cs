using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingReadService(
    IFastingOccurrenceReadModelRepository fastingOccurrenceRepository,
    IFastingCheckInReadModelRepository fastingCheckInRepository,
    IFastingAnalyticsService fastingAnalyticsService,
    TimeProvider dateTimeProvider)
    : IFastingReadService {
    private const int OverviewHistoryPageSize = 10;

    public async Task<FastingSessionModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        FastingOccurrenceReadModel? current = await GetCurrentOccurrenceAsync(userId, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return null;
        }

        IReadOnlyList<FastingCheckInReadModel> checkIns = await GetCheckInsAsync(current, cancellationToken).ConfigureAwait(false);
        return current.ToModel(current.Plan, checkIns);
    }

    public async Task<FastingInsightsModel> GetInsightsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        FastingOccurrenceReadModel? current = await GetCurrentOccurrenceAsync(userId, cancellationToken).ConfigureAwait(false);
        return await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingOverviewModel> GetOverviewAsync(
        UserId userId,
        CancellationToken cancellationToken) {
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

    private Task<FastingOccurrenceReadModel?> GetCurrentOccurrenceAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        fastingOccurrenceRepository.GetCurrentReadModelAsync(userId, cancellationToken);

    private Task<IReadOnlyList<FastingCheckInReadModel>> GetCheckInsAsync(
        FastingOccurrenceReadModel occurrence,
        CancellationToken cancellationToken) =>
        fastingCheckInRepository.GetByOccurrenceIdReadModelsAsync([occurrence.Id], cancellationToken);
}
