using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingAnalyticsService(
    IFastingOccurrenceReadModelRepository fastingOccurrenceRepository,
    IFastingCheckInReadModelRepository fastingCheckInRepository)
    : IFastingAnalyticsService {
    private const int AnalysisDays = 90;

    public (DateTime FromUtc, DateTime ToUtc) GetDefaultHistoryWindow(DateTime nowUtc) {
        var currentMonthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (currentMonthStart.AddMonths(-1), currentMonthStart.AddMonths(2).AddTicks(-1));
    }

    public async Task<FastingStatsModel> GetStatsAsync(UserId userId, DateTime nowUtc, CancellationToken cancellationToken) {
        IReadOnlyList<FastingOccurrenceReadModel> allOccurrences = await fastingOccurrenceRepository.GetByUserReadModelsAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrenceAnalysis> allAnalyses = await BuildAnalysesAsync(allOccurrences, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrenceReadModel> last30Occurrences = await fastingOccurrenceRepository.GetByUserReadModelsAsync(
            userId,
            from: nowUtc.AddDays(-30),
            to: nowUtc,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrenceAnalysis> last30Analyses = await BuildAnalysesAsync(last30Occurrences, cancellationToken).ConfigureAwait(false);
        return FastingStatsCalculator.Create(allOccurrences, allAnalyses, last30Occurrences, last30Analyses, nowUtc);
    }

    public async Task<FastingInsightsModel> GetInsightsAsync(
        UserId userId,
        DateTime nowUtc,
        FastingOccurrenceReadModel? current,
        CancellationToken cancellationToken) {
        IReadOnlyList<FastingOccurrenceReadModel> history = await fastingOccurrenceRepository.GetByUserReadModelsAsync(
            userId,
            from: nowUtc.AddDays(-AnalysisDays),
            to: nowUtc,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrenceAnalysis> analyses = await BuildAnalysesAsync(history, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrenceAnalysis> currentAnalyses = current is null
            ? []
            : await BuildAnalysesAsync([current], cancellationToken).ConfigureAwait(false);
        FastingCheckInSnapshot? currentLatestCheckIn = currentAnalyses.Count > 0 ? currentAnalyses[0].LatestCheckIn : null;

        return new FastingInsightsModel(
            FastingInsightBuilder.BuildAlerts(current, currentLatestCheckIn, nowUtc),
            FastingInsightBuilder.BuildInsights(analyses));
    }

    public async Task<PagedResponse<FastingSessionModel>> GetHistoryAsync(
        UserId userId,
        int page,
        int limit,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) {
        (IReadOnlyList<FastingOccurrenceReadModel> occurrences, int totalItems) = await fastingOccurrenceRepository.GetPagedByUserReadModelsAsync(
            userId,
            page: page,
            limit: limit,
            from: fromUtc,
            to: toUtc,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckInReadModel>> checkInsByOccurrence = await GetCheckInsByOccurrenceAsync(occurrences.Select(static occurrence => occurrence.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        return FastingHistoryResponseBuilder.Build(occurrences, checkInsByOccurrence, page, limit, totalItems);
    }

    private async Task<IReadOnlyList<FastingOccurrenceAnalysis>> BuildAnalysesAsync(
        IReadOnlyList<FastingOccurrenceReadModel> occurrences,
        CancellationToken cancellationToken) {
        if (occurrences.Count == 0) {
            return [];
        }

        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckInReadModel>> checkInsByOccurrence = await GetCheckInsByOccurrenceAsync(occurrences.Select(static occurrence => occurrence.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        return FastingOccurrenceAnalysisBuilder.Build(occurrences, checkInsByOccurrence);
    }

    private async Task<IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckInReadModel>>> GetCheckInsByOccurrenceAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken) {
        if (occurrenceIds.Count == 0) {
            return new Dictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckInReadModel>>();
        }

        IReadOnlyList<FastingCheckInReadModel> checkIns = await fastingCheckInRepository
            .GetByOccurrenceIdReadModelsAsync(occurrenceIds, cancellationToken)
            .ConfigureAwait(false);

        return FastingCheckInLookup.Create(checkIns);
    }
}
