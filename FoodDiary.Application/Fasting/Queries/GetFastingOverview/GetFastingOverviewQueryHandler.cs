using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingOverview;

public sealed class GetFastingOverviewQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository,
    IFastingAnalyticsService fastingAnalyticsService,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingOverviewQuery, Result<FastingOverviewModel>> {
    private const int HistoryPageSize = 10;

    public async Task<Result<FastingOverviewModel>> Handle(GetFastingOverviewQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingOverviewModel>(accessError);
        }

        var now = dateTimeProvider.UtcNow;
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var currentCheckIns = current is null
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync([current.Id], cancellationToken).ConfigureAwait(false);
        var stats = await fastingAnalyticsService.GetStatsAsync(userId, now, cancellationToken).ConfigureAwait(false);
        var insights = await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken).ConfigureAwait(false);
        var (fromUtc, toUtc) = fastingAnalyticsService.GetDefaultHistoryWindow(now);
        var history = await fastingAnalyticsService.GetHistoryAsync(
            userId,
            1,
            HistoryPageSize,
            fromUtc,
            toUtc,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new FastingOverviewModel(
            current?.ToModel(current.Plan, currentCheckIns),
            stats,
            insights,
            history));
    }
}
