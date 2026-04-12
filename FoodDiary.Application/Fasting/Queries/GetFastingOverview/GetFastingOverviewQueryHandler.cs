using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingOverview;

public sealed class GetFastingOverviewQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository,
    IFastingAnalyticsService fastingAnalyticsService,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingOverviewQuery, Result<FastingOverviewModel>> {
    private const int HistoryPageSize = 10;

    public async Task<Result<FastingOverviewModel>> Handle(GetFastingOverviewQuery query, CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        var now = dateTimeProvider.UtcNow;
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken);
        var currentCheckIns = current is null
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync([current.Id], cancellationToken);
        var stats = await fastingAnalyticsService.GetStatsAsync(userId, now, cancellationToken);
        var insights = await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken);
        var (fromUtc, toUtc) = fastingAnalyticsService.GetDefaultHistoryWindow(now);
        var history = await fastingAnalyticsService.GetHistoryAsync(
            userId,
            1,
            HistoryPageSize,
            fromUtc,
            toUtc,
            cancellationToken);

        return Result.Success(new FastingOverviewModel(
            current?.ToModel(current.Plan, currentCheckIns),
            stats,
            insights,
            history));
    }
}
