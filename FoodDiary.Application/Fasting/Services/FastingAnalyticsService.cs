using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingAnalyticsService(
    IFastingOccurrenceReadRepository fastingOccurrenceRepository,
    IFastingCheckInReadRepository fastingCheckInRepository)
    : IFastingAnalyticsService {
    private const int AnalysisDays = 90;

    public (DateTime FromUtc, DateTime ToUtc) GetDefaultHistoryWindow(DateTime nowUtc) {
        var currentMonthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (currentMonthStart.AddMonths(-1), currentMonthStart.AddMonths(2).AddTicks(-1));
    }

    public async Task<FastingStatsModel> GetStatsAsync(UserId userId, DateTime nowUtc, CancellationToken cancellationToken) {
        IReadOnlyList<FastingOccurrence> allOccurrences = await fastingOccurrenceRepository.GetByUserAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrenceAnalysis> allAnalyses = await BuildAnalysesAsync(allOccurrences, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingOccurrence> last30Occurrences = await fastingOccurrenceRepository.GetByUserAsync(
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
        FastingOccurrence? current,
        CancellationToken cancellationToken) {
        IReadOnlyList<FastingOccurrence> history = await fastingOccurrenceRepository.GetByUserAsync(
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
        (IReadOnlyList<FastingOccurrence> occurrences, int totalItems) = await fastingOccurrenceRepository.GetPagedByUserAsync(
            userId,
            page: page,
            limit: limit,
            from: fromUtc,
            to: toUtc,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>> checkInsByOccurrence = await GetCheckInsByOccurrenceAsync(occurrences.Select(static occurrence => occurrence.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        var models = occurrences
            .Select(occurrence => occurrence.ToModel(
                occurrence.Plan,
                checkInsByOccurrence.GetValueOrDefault(occurrence.Id)))
            .ToList();
        int totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)limit);

        return new PagedResponse<FastingSessionModel>(models, page, limit, totalPages, totalItems);
    }

    private async Task<IReadOnlyList<FastingOccurrenceAnalysis>> BuildAnalysesAsync(
        IReadOnlyList<FastingOccurrence> occurrences,
        CancellationToken cancellationToken) {
        if (occurrences.Count == 0) {
            return [];
        }

        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>> checkInsByOccurrence = await GetCheckInsByOccurrenceAsync(occurrences.Select(static occurrence => occurrence.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        return occurrences
            .Select(occurrence => {
                IReadOnlyList<FastingCheckInSnapshot> timeline = FastingCheckInTimelineBuilder.Build(occurrence, checkInsByOccurrence.GetValueOrDefault(occurrence.Id));
                FastingCheckInSnapshot? latestCheckIn = timeline.Count > 0 ? timeline[0] : null;
                return new FastingOccurrenceAnalysis(occurrence, timeline, latestCheckIn);
            })
            .ToList();
    }

    private async Task<IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>>> GetCheckInsByOccurrenceAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken) {
        if (occurrenceIds.Count == 0) {
            return new Dictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>>();
        }

        IReadOnlyList<FastingCheckIn> checkIns = await fastingCheckInRepository
            .GetByOccurrenceIdsAsync(occurrenceIds, cancellationToken)
            .ConfigureAwait(false);

        return FastingCheckInLookup.Create(checkIns);
    }
}
