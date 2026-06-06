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
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Application.Common.Models;

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
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingOverviewModel>(accessError);
        }

        DateTime now = dateTimeProvider.UtcNow;
        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FastingCheckIn> currentCheckIns = current is null
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync([current.Id], cancellationToken).ConfigureAwait(false);
        FastingStatsModel stats = await fastingAnalyticsService.GetStatsAsync(userId, now, cancellationToken).ConfigureAwait(false);
        FastingInsightsModel insights = await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken).ConfigureAwait(false);
        (DateTime fromUtc, DateTime toUtc) = fastingAnalyticsService.GetDefaultHistoryWindow(now);
        PagedResponse<FastingSessionModel> history = await fastingAnalyticsService.GetHistoryAsync(
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
